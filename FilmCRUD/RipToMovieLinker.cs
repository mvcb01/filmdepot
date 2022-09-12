using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Polly;

using ConfigUtils.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;
using Polly.Wrap;

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

            // to save the new Movie entities linked to some MovieRip
            var newMovieEntities = new List<Movie>();

            // maps MovieRip.Id -> Movie.Id
            var ripToMovieMapping = new Dictionary<int, int>();

            // policies; notice the order of the async policies when calling Policy.WrapAsync
            IRateLimitPolicyConfig rateLimitConfig = this._appSettingsManager.GetRateLimitPolicyConfig();
            IRetryPolicyConfig retryConfig = this._appSettingsManager.GetRetryPolicyConfig();
            AsyncPolicyWrap policyWrap = Policy.WrapAsync(
                //PolicyBuilder.
            );



        }

        public async Task SearchAndLinkAsync_OLD()
        {
            IEnumerable<MovieRip> ripsToLink = GetMovieRipsToLink();

            if (!ripsToLink.Any())
            {
                return;
            }

            var ripsForOnlineSearch = new List<MovieRip>();
            var errors = new List<string>();

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

            // online search task associated with each movieRip.FileName
            var newMovieEntitiesTasks = new Dictionary<string, Task<Movie>>();
            foreach (var movieRip in ripsForOnlineSearch)
            {
                Task<Movie> onlineSearchTask = Task.Run<Movie>(async () =>
                {
                    IEnumerable<MovieSearchResult> searchResultAll = await _movieAPIClient.SearchMovieAsync(movieRip.ParsedTitle);
                    return PickMovieFromSearchResults(searchResultAll, movieRip.ParsedTitle, movieRip.ParsedReleaseDate);
                });

                newMovieEntitiesTasks.Add(movieRip.FileName, onlineSearchTask);
            }

            try
            {
                await Task.WhenAll(newMovieEntitiesTasks.Values);
            }
            // exceptions thrown in method PickMovieFromSearchResults
            catch (Exception ex) when (ex is NoSearchResultsError || ex is MultipleSearchResultsError) {}

            foreach (Task task in newMovieEntitiesTasks.Values.Where(t => !t.IsCompletedSuccessfully))
            {
                Exception innerExc = task.Exception.InnerException;
                errors.Add($"ex message: {innerExc.GetType().Name}: {innerExc.Message}");
            }

            // different searches may have returned the "same" Movie, we choose one Movie entity for each
            // distinct externalid
            var newMovieEntities = newMovieEntitiesTasks.Values
                .Where(t => t.IsCompletedSuccessfully)
                .Select(t => t.Result)
                .GroupBy(m => m.ExternalId)
                .Select(group => group.First());
            foreach (var movieRip in ripsForOnlineSearch)
            {
                int linkedMovieExternalId = newMovieEntitiesTasks[movieRip.FileName].Result.ExternalId;
                movieRip.Movie = newMovieEntities.Where(m => m.ExternalId == linkedMovieExternalId).First();
            }

            PersistErrorInfo("linking_errors.txt", errors);

            _unitOfWork.Complete();
        }

        public async Task LinkFromManualExternalIdsAsync()
        {
            Dictionary<string, int> allManualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();

            // filtering the manual configuration to consider only movierips whose filename exists in the repo
            IEnumerable<string> ripFileNamesInRepo = _unitOfWork.MovieRips.GetAll().GetFileNames();
            Dictionary<string, int> manualExternalIds = allManualExternalIds
                .Where(kvp => ripFileNamesInRepo.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // we'll only use the api client for those external ids that do not have a matching external id
            // in the movie repo
            IEnumerable<int> externalIdsForApiCalls = manualExternalIds
                .Select(kvp => kvp.Value)
                .Distinct()
                .Where(_id => _unitOfWork.Movies.FindByExternalId(_id) == null);

            var onlineInfoTasks = new List<Task<MovieSearchResult>>();
            foreach (var externalId in externalIdsForApiCalls)
            {
                onlineInfoTasks.Add(this._movieAPIClient.GetMovieInfoAsync(externalId));
            }
            await Task.WhenAll(onlineInfoTasks);

            // explicit casting is defined in class MovieSearchResult; also, we call ToList to
            // force an eager operation; this way it is guaranteed that two MovieRips will
            // map to the same Movie object if they have the same manual external id;
            IEnumerable<Movie> newMovies = onlineInfoTasks.Select(t => (Movie)t.Result).ToList();

            foreach (var item in manualExternalIds)
            {
                MovieRip ripToLink = _unitOfWork.MovieRips.FindByFileName(item.Key);

                if (externalIdsForApiCalls.Contains(item.Value))
                {
                    ripToLink.Movie = newMovies.Where(m => m.ExternalId == item.Value).First();
                }
                else
                {
                    ripToLink.Movie = _unitOfWork.Movies.FindByExternalId(item.Value);
                }
            }

            _unitOfWork.Complete();

        }

        public IEnumerable<string> GetAllUnlinkedMovieRips()
        {
            return this._unitOfWork.MovieRips.Find(m => m.Movie == null).GetFileNames();
        }

        public async Task<Dictionary<string, Dictionary<string, int>>> ValidateManualExternalIdsAsync()
        {
            Dictionary<string, int> manualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();

            var validationTasks = new Dictionary<string, Task<bool>>();
            foreach (var item in manualExternalIds)
            {
                validationTasks.Add(item.Key, this._movieAPIClient.ExternalIdExistsAsync(item.Value));
            }

            await Task.WhenAll(validationTasks.Values);

            // keys of the original dict `manualExternalIds` for valid external ids
            IEnumerable<string> validIdKeys = validationTasks.Where(kvp => kvp.Value.Result).Select(kvp => kvp.Key);

            return new Dictionary<string, Dictionary<string, int>>() {
                ["valid"] = manualExternalIds.Where(kvp => validIdKeys.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["invalid"] = manualExternalIds.Where(kvp => !validIdKeys.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
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

        private void PersistErrorInfo(string filename, IEnumerable<string> errors)
        {
            if (!errors.Any()) { return; }

            string errorsFpath = Path.Combine(this._appSettingsManager.GetWarehouseContentsTextFilesDirectory(), filename);
            System.Console.WriteLine($"Erros no linking, consultar o seguinte ficheiro: {errorsFpath}");
            this._fileSystemIOWrapper.WriteAllText(errorsFpath, string.Join("\n\n", errors));
        }

    }

}