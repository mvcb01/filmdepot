using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Polly;
using Polly.Wrap;
using Polly.RateLimit;

using ConfigUtils.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Interfaces;
using FilmCRUD.Helpers;
using MovieAPIClients;
using MovieAPIClients.Interfaces;
using CommandLine;

namespace FilmCRUD
{
    public class RipToMovieLinker
    {
        private IFileSystemIOWrapper _fileSystemIOWrapper { get; init; }

        private IUnitOfWork _unitOfWork { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        private IMovieAPIClient _movieAPIClient { get; init; }

        public RipToMovieLinker(
            IUnitOfWork unitOfWork,
            IFileSystemIOWrapper fileSystemIOWrapper,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
            this._fileSystemIOWrapper = fileSystemIOWrapper;
            this._appSettingsManager = appSettingsManager;
            this._movieAPIClient = movieAPIClient;
        }

        /// <summary>
        /// Gets MovieRips not linked to a Movie, excluding RipFilenamesToIgnore and also those with ManualExternalIds
        /// </summary>
        public IEnumerable<MovieRip> GetMovieRipsToLink()
        {
            IEnumerable<string> toIgnore = _appSettingsManager.GetRipFilenamesToIgnoreOnLinking();
            Dictionary<string, int> manualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();
            IEnumerable<string> ripNamesToExclude = Enumerable.Concat<string>(toIgnore, manualExternalIds.Keys);
            // using Find before Where so we limit the number of objects loaded into memory
            return _unitOfWork.MovieRips
                .Find(r => r.Movie == null)
                .Where(r => r.ParsedTitle != null && !ripNamesToExclude.Contains(r.FileName));
        }

        public Movie FindRelatedMovieEntityInRepo(MovieRip movieRip)
        {

            Movie relatedMovie = null;

            // we'll have ripReleaseDate == 0 and parsed == false whenever movieRip.ParsedReleaseDate == null
            bool releaseDateParsed = int.TryParse(movieRip.ParsedReleaseDate, out int ripReleaseDate);

            IEnumerable<Movie> existingMatches = this._unitOfWork.Movies.SearchMoviesWithTitle(movieRip.ParsedTitle);
            int matchCount = existingMatches.Count();
            if (matchCount == 1)
            {
                relatedMovie = existingMatches.First();
            }
            else if (matchCount > 1)
            {
                if (!releaseDateParsed)
                {
                    throw new MultipleMovieMatchesError(
                        $"Several matches in Movie repository for \"{movieRip.FileName}\" with Title = \"{movieRip.ParsedTitle}\"; count = {matchCount}"
                        );
                }
                else
                {
                    IEnumerable<Movie> existingMatchesWithDate = existingMatches.Where(m => m.ReleaseDate == ripReleaseDate);
                    int matchCountWithDate = existingMatchesWithDate.Count();
                    if (matchCountWithDate > 1)
                    {
                        throw new MultipleMovieMatchesError(
                            $"Several matches in Movie repository for \"{movieRip.FileName}\" with Title = \"{movieRip.ParsedTitle}\" and ReleaseDate = {ripReleaseDate}; count = {matchCount}"
                            );
                    }
                    else if (matchCountWithDate == 1)
                    {
                        relatedMovie = existingMatchesWithDate.First();
                    }
                }
            }

            return relatedMovie;
        }

        public async Task SearchAndLinkAsync()
        {
            IEnumerable<MovieRip> ripsToLink = GetMovieRipsToLink();

            if (!ripsToLink.Any())
            {
                return;
            }

            var ripsForOnlineSearch = new List<MovieRip>();
            var errors = new List<string>();

            // some movie rips may already have a match in some existing Movie entity
            foreach (var movieRip in ripsToLink)
            {
                try
                {
                    Movie movie = FindRelatedMovieEntityInRepo(movieRip);
                    if (movie != null)
                    {
                        movieRip.Movie = movie;
                    }
                    else
                    {
                        ripsForOnlineSearch.Add(movieRip);
                    }
                }
                // exceptions thrown in FindRelatedMovieEntityInRepo
                catch (MultipleMovieMatchesError ex)
                {
                    var msg = $"MultipleMovieMatchesError: {movieRip.FileName}: \n{ex.Message}";
                    errors.Add(msg);
                }
            }

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            // to save the new Movie entities linked to some MovieRip
            var newMovieEntities = new List<Movie>();

            // TODO: investigate how to properly use the limit+retry policy with Task.WhenAll...
            foreach (var movieRip in ripsForOnlineSearch)
            {
                try
                {
                    IEnumerable<MovieSearchResult> searchResults = await policyWrap.ExecuteAsync(
                        () => _movieAPIClient.SearchMovieAsync(movieRip.ParsedTitle)
                    );
                    Movie movieToLink = PickMovieFromSearchResults(searchResults, movieRip.ParsedTitle, movieRip.ParsedReleaseDate);

                    // we may already have the "same" Movie from a previous searched
                    Movie existingMovie = newMovieEntities.Where(m => m.ExternalId == movieToLink.ExternalId).FirstOrDefault();
                    
                    if (existingMovie != null)
                    {
                        movieRip.Movie = existingMovie;
                    }
                    else
                    {
                        movieRip.Movie = movieToLink;
                        newMovieEntities.Add(movieToLink);
                    }
                }
                // in case we exceed IRetryPolicyConfig.RetryCount; no need to throw again, just let the others run
                catch (RateLimitRejectedException ex)
                {
                    errors.Add($"Rate Limit error for {movieRip.FileName}; Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                // exceptions thrown in FindRelatedMovieEntityInRepo; used for control flow, probably shouldn't...
                catch (Exception ex) when (ex is NoSearchResultsError || ex is MultipleSearchResultsError)
                {
                    errors.Add($"Linking error for {movieRip.FileName}: {ex.Message}");
                }
                // abort for other exceptions, entity changes are not persisted
                catch (Exception)
                {
                    this._unitOfWork.Dispose();
                    throw;
                }
            }

            PersistErrorInfo("linking_errors.txt", errors);

            this._unitOfWork.Complete();
        }

        public async Task LinkFromManualExternalIdsAsync()
        {
            Dictionary<string, int> allManualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();

            if (!allManualExternalIds.Any())
            {
                return;
            }

            // filtering the manual configuration to consider only movierips whose filename exists in the repo
            IEnumerable<string> ripFileNamesInRepo = this._unitOfWork.MovieRips.GetAll().GetFileNames();
            Dictionary<string, int> manualExternalIds = allManualExternalIds
                .Where(kvp => ripFileNamesInRepo.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // we'll only use the api client for those external ids that do not have a matching external id
            // in the movie repo
            IEnumerable<int> externalIdsForApiCalls = manualExternalIds
                .Select(kvp => kvp.Value)
                .Distinct()
                .Where(_id => this._unitOfWork.Movies.FindByExternalId(_id) == null);

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            var newMovies = new List<Movie>();
            var rateLimitErrors = new List<string>();
            foreach (int externalId in externalIdsForApiCalls)
            {
                try
                {
                    MovieSearchResult movieInfo = await this._movieAPIClient.GetMovieInfoAsync(externalId);
                    // explicit casting is defined
                    newMovies.Add((Movie)movieInfo);
                }
                // in case we exceed IRetryPolicyConfig.RetryCount; no need to throw again, just let the others run
                catch (RateLimitRejectedException ex)
                {
                    rateLimitErrors.Add($"Rate Limit error for external id {externalId}; Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                // abort for other exceptions, entity changes are not persisted
                catch (Exception)
                {
                    this._unitOfWork.Dispose();
                    throw;
                }
            }

            // links each manually configured movierip to a new Movie entity or to an existing one
            foreach (var item in manualExternalIds)
            {
                MovieRip ripToLink = this._unitOfWork.MovieRips.FindByFileName(item.Key);

                if (externalIdsForApiCalls.Contains(item.Value))
                {
                    ripToLink.Movie = newMovies.Where(m => m.ExternalId == item.Value).First();
                }
                else
                {
                    ripToLink.Movie = this._unitOfWork.Movies.FindByExternalId(item.Value);
                }
            }
            
            PersistErrorInfo("manual_linking_errors.txt", rateLimitErrors);

            this._unitOfWork.Complete();
        }

        public IEnumerable<string> GetAllUnlinkedMovieRips()
        {
            return this._unitOfWork.MovieRips.Find(m => m.Movie == null).GetFileNames();
        }

        public async Task<Dictionary<string, Dictionary<string, int>>> ValidateManualExternalIdsAsync()
        {
            Dictionary<string, int> manualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            var validIds = new List<int>();
            var rateLimitErrors = new List<string>();
            foreach (var item in manualExternalIds)
            {
                try
                {
                    if (await this._movieAPIClient.ExternalIdExistsAsync(item.Value)) validIds.Add(item.Value);   
                }
                // in case we exceed IRetryPolicyConfig.RetryCount; no need to throw again, just let the others run//
                catch (RateLimitRejectedException ex)
                {
                    rateLimitErrors.Add($"Rate Limit error while validating external id {item.Value}; Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
            }

            PersistErrorInfo("external_ids_validation_errors.txt", rateLimitErrors);

            return new Dictionary<string, Dictionary<string, int>>() {
                ["valid"] = manualExternalIds.Where(kvp => validIds.Contains(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["invalid"] = manualExternalIds.Where(kvp => !validIds.Contains(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        public static Movie PickMovieFromSearchResults(IEnumerable<MovieSearchResult> searchResultAll, string parsedTitle, string parsedReleaseDate = null)
        {
            // filters results using both Title and OriginalTitle
            IEnumerable<string> titleTokens = parsedTitle.GetStringTokensWithoutPunctuation();
            List<MovieSearchResult> searchResult = searchResultAll
                .Where(r => titleTokens.SequenceEqual(r.Title.GetStringTokensWithoutPunctuation())
                    ||
                    titleTokens.SequenceEqual(r.OriginalTitle.GetStringTokensWithoutPunctuation()))
                .ToList();

            int resultCount = searchResult.Count();
            MovieSearchResult result;
            if (resultCount == 0)
            {
                throw new NoSearchResultsError($"No search results for \"{parsedTitle}\" ");
            }
            else if (resultCount == 1)
            {
                result = searchResult.First();
            }
            else if (parsedReleaseDate == null)
            {
                throw new MultipleSearchResultsError($"Multiple search results for \"{parsedTitle}\"; count = {resultCount}");
            }
            else
            {
                bool parseSuccess = int.TryParse(parsedReleaseDate, out int releaseDate);
                string[] admissibleDates;
                if (parseSuccess)
                {
                    admissibleDates = new string[] {
                        releaseDate.ToString(),
                        (releaseDate + 1).ToString(),
                        (releaseDate - 1).ToString()
                    };
                }
                else
                {
                    admissibleDates = new string[] { parsedReleaseDate };
                }

                List<MovieSearchResult> searchResultFiltered = searchResult
                    .Where(r => admissibleDates.Contains(r.ReleaseDate.ToString()))
                    .ToList();
                int resultCountFiltered = searchResultFiltered.Count();

                if (resultCountFiltered == 0)
                {
                    throw new NoSearchResultsError(
                        $"No search results for \"{parsedTitle}\" with release date in {string.Join(", ", admissibleDates)}");
                }
                else if (resultCountFiltered > 1)
                {
                    throw new MultipleSearchResultsError(
                        $"Multiple search results for \"{parsedTitle}\"  with release date in {string.Join(", ", admissibleDates)}; count = {resultCount}");
                }
                result = searchResultFiltered.First();
            }

            // explicit conversion is defined
            return (Movie)result;
        }

        private AsyncPolicyWrap GetPolicyWrapFromConfigs(out TimeSpan initialDelay)
        {
            // notice the order of the async policies when calling Policy.WrapAsync:
            //      outermost (at left) to innermost (at right)
            IRateLimitPolicyConfig rateLimitConfig = this._appSettingsManager.GetRateLimitPolicyConfig();
            IRetryPolicyConfig retryConfig = this._appSettingsManager.GetRetryPolicyConfig();

            initialDelay = rateLimitConfig.PerTimeSpan;
            return Policy.WrapAsync(
                APIClientPolicyBuilder.GetAsyncRetryPolicy<RateLimitRejectedException>(retryConfig),
                APIClientPolicyBuilder.GetAsyncRateLimitPolicy(rateLimitConfig)
            );
        }

        private void PersistErrorInfo(string filename, IEnumerable<string> errors)
        {
            if (!errors.Any()) { return; }

            string errorsFpath = Path.Combine(this._appSettingsManager.GetWarehouseContentsTextFilesDirectory(), filename);
            System.Console.WriteLine($"Linking errors, details in: {errorsFpath}");
            this._fileSystemIOWrapper.WriteAllText(errorsFpath, string.Join("\n\n", errors));
        }

    }

}