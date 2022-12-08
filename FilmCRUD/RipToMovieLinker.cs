using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Polly;
using Polly.Wrap;
using Polly.RateLimit;
using System.Net.Http;
using System.Net;
using Serilog;
using ConfigUtils.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Helpers;
using MovieAPIClients;
using MovieAPIClients.Interfaces;
using System.Reflection;

namespace FilmCRUD
{
    public class RipToMovieLinker
    {
        private IUnitOfWork _unitOfWork { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        private IMovieAPIClient _movieAPIClient { get; init; }

        private readonly ILogger _linkingErrorsLogger;

        public RipToMovieLinker(IUnitOfWork unitOfWork, IAppSettingsManager appSettingsManager, IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
            this._appSettingsManager = appSettingsManager;
            this._movieAPIClient = movieAPIClient;
        }


        public RipToMovieLinker(
            IUnitOfWork unitOfWork,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient,
            ILogger linkingErrorsLogger) : this(unitOfWork, appSettingsManager, movieAPIClient) => this._linkingErrorsLogger = linkingErrorsLogger;
        

        public async Task SearchAndLinkAsync(int maxApiCalls = -1)
        {   
            IEnumerable<MovieRip> ripsToLink = GetMovieRipsToLink();
            int totalCount = ripsToLink.Count();

            Log.Information("MovieRips to link - total count: {TotalCount}", totalCount);

            if (totalCount == 0) return;

            var ripsForOnlineSearch = new List<MovieRip>();
            int foundLocallyCount = 0;
            var errors = new List<string>();

            var logStepLocalSearch = (int)Math.Ceiling((decimal)totalCount / 20.0m);

            Log.Information("Finding movies to link - locally...");
            // some movie rips may already have a match in some existing Movie entity
            foreach (var (movieRip, idx) in ripsToLink.Select((value, idx) => (value, idx + 1)))
            {
                try
                {
                    Movie movieToLink = FindRelatedMovieEntityInRepo(movieRip);
                    if (movieToLink != null)
                    {
                        Log.Debug("FOUND LOCALLY: {FileName} -> {Movie}", movieRip.FileName, movieToLink.ToString());
                        movieRip.Movie = movieToLink;
                        foundLocallyCount++;
                    }
                    else
                    {
                        Log.Debug("NOT FOUND LOCALLY: {FileName}", movieRip.FileName);
                        ripsForOnlineSearch.Add(movieRip);
                    }
                }
                // exceptions thrown in FindRelatedMovieEntityInRepo
                catch (MultipleMovieMatchesError ex)
                {
                    Log.Debug("NOT FOUND, MULTIPLE MATCHES IN LOCAL REPO: {FileName}", movieRip.FileName);
                    errors.Add(ex.Message);
                }

                if (idx % logStepLocalSearch == 0 || idx == totalCount)
                {
                    Log.Information("Finding movies to link - locally: {Index} out of {Total}", idx, totalCount);
                }
            }

            int onlineSearchCount = ripsForOnlineSearch.Count;
            Log.Information("Total count for online search: {OnlineSearchCount}", onlineSearchCount);

            if (0 < maxApiCalls && maxApiCalls < onlineSearchCount)
            {
                Log.Information("Limiting number of rips for online search to {CallLimit}", maxApiCalls);
                ripsForOnlineSearch = ripsForOnlineSearch.Take(maxApiCalls).ToList();
                onlineSearchCount = maxApiCalls;
            }

            var logStepOnlineSearch = (int)Math.Ceiling((decimal)onlineSearchCount / 20.0m);
            
            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            // to save the new Movie entities linked to some MovieRip
            var newMovieEntities = new List<Movie>();

            int foundOnlineCount = 0;
            
            // TODO: investigate how to properly use the limit+retry policy with Task.WhenAll...
            Log.Information("Finding movies to link - online...");
            Log.Information("API base address: {ApiBaseAddress}", this._movieAPIClient.ApiBaseAddress);
            foreach (var (movieRip, idx) in ripsForOnlineSearch.Select((value, idx) => (value, idx + 1)))
            {
                try
                {
                    Movie movieToLink = await SearchMovieAndPickFromResultsAsync(movieRip, policyWrap);

                    Log.Debug("FOUND: {FileName} -> {Movie}", movieRip.FileName, movieToLink.ToString());

                    foundOnlineCount++;

                    // we may already have the "same" Movie from a previous api call in this for loop
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
                    Log.Debug("RATE LIMIT ERROR: {FileName}", movieRip.FileName);
                    errors.Add($"RATE LIMIT ERROR: {movieRip.FileName}; Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                // exceptions thrown in FindRelatedMovieEntityInRepo; used for control flow, probably shouldn't...
                catch (Exception ex) when (ex is NoSearchResultsError || ex is MultipleSearchResultsError)
                {
                    Log.Debug("LINKING ERROR: {FileName}", movieRip.FileName);
                    errors.Add($"LINKING ERROR: {movieRip.FileName}: {ex.Message}");
                }
                // abort for other exceptions, entity changes are not persisted
                catch (Exception ex)
                {
                    Log.Fatal(ex.Message);
                    this._unitOfWork.Dispose();
                    throw;
                }

                if (idx % logStepOnlineSearch == 0 || idx == onlineSearchCount)
                {
                    Log.Information("Finding movies to link - online: {Index} out of {Total}", idx, onlineSearchCount);
                }
            }

            int errorCount = errors.Count();
            if (errorCount > 0)
            {
                Log.Information("Saving linking errors in separate file...");
                this._linkingErrorsLogger?.Information("----------------------------------");
                this._linkingErrorsLogger?.Information("--- {DateTime} ---", DateTime.Now.ToString("g"));
                this._linkingErrorsLogger?.Information("----------------------------------");
                errors.ForEach(e => this._linkingErrorsLogger?.Error(e));
            }

            Log.Information("----------- LINK SUMMARY -----------");
            Log.Information("Movies found locally: {FoundLocallyCount}", foundLocallyCount);
            Log.Information("Movies found online: {FoundOnlineCount}", foundOnlineCount);
            Log.Information("Linking errors: {ErrorCount}", errorCount);
            Log.Information("------------------------------------");

            this._unitOfWork.Complete();
        }

        public async Task LinkFromManualExternalIdsAsync(int maxApiCalls = -1)
        {
            Dictionary<string, int> allManualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();

            int allManualExternalIdsCount = allManualExternalIds.Count();

            Log.Information("Manually configured external ids - count: {AllManualExternalIdsCount}", allManualExternalIdsCount);

            if (allManualExternalIdsCount == 0) return;

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

            int externalIdsForApiCallsCount = externalIdsForApiCalls.Count();
            var logStepApiCalls = (int)Math.Ceiling((decimal)externalIdsForApiCallsCount / 20.0m);

            Log.Information("External ids for API calls - count: {ExternalIdsForApiCallsCount}", externalIdsForApiCallsCount);

            if (0 < maxApiCalls && maxApiCalls < externalIdsForApiCallsCount)
            {
                Log.Information("Limiting number of rips for online search to {CallLimit}", maxApiCalls);
                externalIdsForApiCalls = externalIdsForApiCalls.Take(maxApiCalls).ToList();
                externalIdsForApiCallsCount = maxApiCalls;
            }

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            var newMovies = new List<Movie>();
            var errors = new Dictionary<int, string>();

            Log.Information("Finding movies from manual external ids - online...");
            Log.Information("API base address: {ApiBaseAddress}", this._movieAPIClient.ApiBaseAddress);
            foreach (var (externalId, idx) in externalIdsForApiCalls.Select((value, idx) => (value, idx + 1)))
            {
                try
                {
                    MovieSearchResult movieInfo = await policyWrap.ExecuteAsync(() => this._movieAPIClient.GetMovieInfoAsync(externalId));
                    
                    // explicit casting is defined
                    Movie movie = (Movie)movieInfo;
                    newMovies.Add(movie);

                    Log.Debug("FOUND: ExternalId = {ExternalId} -> {Movie}", externalId, movie.ToString());
                }
                // in case we exceed IRetryPolicyConfig.RetryCount; no need to throw again, just let the others run
                catch (RateLimitRejectedException ex)
                {
                    Log.Debug("RATE LIMIT ERROR: ExternalId = {ExternalId}", externalId);
                    errors.Add(
                        externalId,
                        $"RATE LIMIT ERROR: ExternalId = {externalId}; Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                // invalid external ids should not stop execution
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Debug("NOT FOUND: ExternalId = {ExternalId}", externalId);
                    errors.Add(externalId, $"NOT FOUND: ExternalId = {externalId}; {ex.Message}");
                }
                // abort for other exceptions, entity changes are not persisted
                catch (Exception ex)
                {
                    Log.Fatal(ex.Message);
                    this._unitOfWork.Dispose();
                    throw;
                }

                if (idx % logStepApiCalls == 0 || idx == externalIdsForApiCallsCount)
                {
                    Log.Information("Finding movies from external ids - online: {Index} out of {Total}", idx, externalIdsForApiCallsCount);
                }
            }

            int manualExternalIdsCount = manualExternalIds.Count();
            int logStep = (int)Math.Ceiling((decimal)manualExternalIdsCount / 20.0m);

            Log.Information("Linking MovieRips to movies from manual external ids...");
            // links each manually configured movierip to a new Movie entity or to an existing one
            foreach (var (item, idx) in manualExternalIds.Select((value, idx) => (value, idx + 1)))
            {
                // ignore external ids that caused ratelimit or notfound errors
                if (errors.ContainsKey(item.Value))
                {
                    Log.Debug("Skipping External id = {ExternalId}...", item.Value);
                    continue;
                }

                MovieRip ripToLink = this._unitOfWork.MovieRips.FindByFileName(item.Key);

                if (externalIdsForApiCalls.Contains(item.Value))
                {
                    Log.Debug("FOUND ONLINE: linking to movie with External id = {ExternalId} - {FileName}", item.Value, item.Key);
                    ripToLink.Movie = newMovies.Where(m => m.ExternalId == item.Value).First();
                }
                else
                {
                    Log.Debug("FOUND LOCALLY: linking to movie with External id = {ExternalId} - {FileName}", item.Value, item.Key);
                    ripToLink.Movie = this._unitOfWork.Movies.FindByExternalId(item.Value);
                }

                if (idx % logStep == 0 || idx == manualExternalIdsCount)
                {
                    Log.Information("Linking MovieRips to movies from manual external ids: {Index} out of {Total}", idx, manualExternalIdsCount);
                }
            }

            int errorCount = errors.Count();
            if (errorCount > 0)
            {
                Log.Information("Saving manual external ids linking errors in separate log file...");
                this._linkingErrorsLogger?.Information("----------------------------------");
                this._linkingErrorsLogger?.Information("--- {DateTime} ---", DateTime.Now.ToString("g"));
                this._linkingErrorsLogger?.Information("----------------------------------");
                errors.Values.ToList().ForEach(e => this._linkingErrorsLogger?.Error(e));
            }

            Log.Information("------- MANUAL EXTERNAL IDS LINK SUMMARY -------");
            Log.Information("Total manual external ids: {ManualExternalIdsCount}", manualExternalIdsCount);
            Log.Information("New movies found online: {FoundOnlineCount}", newMovies.Count());
            Log.Information("Api call errors: {ErrorCount}", errorCount);
            Log.Information("------------------------------------------------");

            this._unitOfWork.Complete();
        }


        public async Task ValidateManualExternalIdsAsync(int maxApiCalls = -1)
        {
            Dictionary<string, int> manualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();

            int manualExternalIdsCount = manualExternalIds.Count();

            if (manualExternalIdsCount == 0)
            {
                Log.Information("No manually configured external ids");
                return;
            }

            if (0 < maxApiCalls && maxApiCalls < manualExternalIdsCount)
            {
                Log.Information("Limiting number of API calls to {CallLimit}", maxApiCalls);
                manualExternalIds = manualExternalIds.Take(maxApiCalls).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                manualExternalIdsCount = maxApiCalls;
            }

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            int validCount = 0;
            Log.Information("validating manually configured external ids...");
            Log.Information("API base address: {ApiBaseAddress}", this._movieAPIClient.ApiBaseAddress);
            foreach (var item in manualExternalIds)
            {
                try
                {
                    bool isValid = await policyWrap.ExecuteAsync(() => this._movieAPIClient.ExternalIdExistsAsync(item.Value));
                    if (isValid)
                    {
                        validCount++;
                        Log.Information("VALID: {FileName} - {ExternalId}", item.Key, item.Value);
                    }
                    else
                    {
                        Log.Information("INVALID: {FileName} - {ExternalId}", item.Key, item.Value);
                    }
                }
                // in case we exceed IRetryPolicyConfig.RetryCount; no need to throw again, just let the others run
                // also no need to save them for later persistence, standard logs should suffice since not too many manual ext ids are expected
                catch (RateLimitRejectedException ex)
                {
                    Log.Information(
                        "Rate limit error: Rate Limit error while validating external id {FileName} - {ExternalId}; Retry after milliseconds {RetryAfter}; Message: {Msg}",
                        item.Key,
                        item.Value,
                        ex.RetryAfter.TotalMilliseconds,
                        ex.Message);
                }
            }

            Log.Information("------- VALIDATE MANUAL EXTERNAL IDS SUMMARY -------");
            Log.Information("Total manual external ids: {ManualExternalIdsCount}", manualExternalIdsCount);
            Log.Information("Valid: {ValidCount}", validCount);
            Log.Information("Invalid: {ValidCount}", manualExternalIdsCount - validCount);
            Log.Information("------------------------------------------------");
        }

        // AsyncPolicyWrap needs to be passed as a param so that the same object is used in every method call;
        // if an AsyncPolicyWrap object was created in the method body then it would serve no purpose
        public async Task<Movie> SearchMovieAndPickFromResultsAsync(MovieRip movieRip, AsyncPolicyWrap policyWrap)
        {
            string parsedTitle = movieRip.ParsedTitle;
            string parsedReleaseDate = movieRip.ParsedReleaseDate;

            MovieSearchResult result = parsedReleaseDate is not null ?
                await SearchAndPickAsync(policyWrap, parsedTitle, parsedReleaseDate) :
                await SearchAndPickAsync(policyWrap, parsedTitle);

            // explicit conversion is defined
            return (Movie)result;
        }

        public IEnumerable<string> GetAllUnlinkedMovieRips() => this._unitOfWork.MovieRips.Find(m => m.Movie == null).GetFileNames();

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
            IEnumerable<string> titleTokens = movieRip.ParsedTitle.GetStringTokensWithoutPunctuation();

            IEnumerable<Movie> allMatches = this._unitOfWork.Movies.SearchMoviesWithTitle(movieRip.ParsedTitle);
            IEnumerable<Movie> tokenFilteredMatches = allMatches.Where(
                mr => titleTokens.SequenceEqual(mr.Title.GetStringTokensWithoutPunctuation(removeDiacritics: true))
                    || titleTokens.SequenceEqual(mr.OriginalTitle.GetStringTokensWithoutPunctuation(removeDiacritics: true))
            );

            int tokenFilteredMatchesCount = tokenFilteredMatches.Count();

            // returns null if empty, the only match if singleton
            if (tokenFilteredMatchesCount < 2)
                return tokenFilteredMatches.FirstOrDefault();

            // for cases where we have more than one title match we'll use the date tolerance nested class

            var dateTol = new ReleaseDateToleranceNeighbourhood(movieRip.ParsedReleaseDate);

            // throwing when we cannot parse the release date to filter the results further
            if (!dateTol.ParseSuccess)
                throw new MultipleMovieMatchesError(
                    $"Several matches in Movie repository for \"{movieRip.FileName}\" with Title = \"{movieRip.ParsedTitle}\"; count = {tokenFilteredMatchesCount}"
                );

            // giving priority to exact date matches
            IEnumerable<Movie> withExactDateMatch = tokenFilteredMatches.Where(m => m.ReleaseDate == dateTol.ReleaseDate);
            int withExactDateMatchCount = withExactDateMatch.Count();

            if (withExactDateMatchCount == 1)
                return withExactDateMatch.First();

            if (withExactDateMatchCount > 1)
                throw new MultipleMovieMatchesError(
                    $"Several matches in Movie repository for \"{movieRip.FileName}\" with Title = \"{movieRip.ParsedTitle}\" and ReleaseDate = {movieRip.ParsedReleaseDate}; count = {withExactDateMatchCount}"
                );

            // trying for matches where the release date is close but no equal to the parsed release date from the MovieRip entity
            IEnumerable<Movie> withDateWithinTolerance = tokenFilteredMatches.Where(m => dateTol.Neighbourhood.Contains(m.ReleaseDate));
            int withDateWithinToleranceCount = withDateWithinTolerance.Count();

            if (withDateWithinToleranceCount == 1)
                return withDateWithinTolerance.First();

            if (withDateWithinToleranceCount > 1)
                throw new MultipleMovieMatchesError(
                        $"Several matches in Movie repository for \"{movieRip.FileName}\" with Title = \"{movieRip.ParsedTitle}\" and ReleaseDate in {string.Join(", ", dateTol.Neighbourhood)}; count = {withExactDateMatchCount}"
                    );
            
            // no match
            return null;
        }


        public AsyncPolicyWrap GetPolicyWrapFromConfigs(out TimeSpan initialDelay)
        {
            // notice the order of the async policies when calling Policy.WrapAsync:
            //      outermost (at left) to innermost (at right)
            IRateLimitPolicyConfig rateLimitConfig = this._appSettingsManager.GetRateLimitPolicyConfig();
            IRetryPolicyConfig retryConfig = this._appSettingsManager.GetRetryPolicyConfig();

            Log.Information("------- API Client Policies -------");
            Log.Information(
                "Rate limit: maximum of {ExecutionCount} calls every {MS} milliseconds; max burst = {MaxBurst}",
                rateLimitConfig.NumberOfExecutions,
                rateLimitConfig.PerTimeSpan.TotalMilliseconds,
                rateLimitConfig.MaxBurst);
            Log.Information(
                "Retry: maximum of {MaxRetry} retries, wait {Sleep} milliseconds between consecutive retries",
                retryConfig.RetryCount,
                retryConfig.SleepDuration.TotalMilliseconds);
            Log.Information("-----------------------------------");

            initialDelay = rateLimitConfig.PerTimeSpan;
            return Policy.WrapAsync(
                APIClientPolicyBuilder.GetAsyncRetryPolicy<RateLimitRejectedException>(retryConfig),
                APIClientPolicyBuilder.GetAsyncRateLimitPolicy(rateLimitConfig)
            );
        }

        private async Task<MovieSearchResult> SearchAndPickAsync(AsyncPolicyWrap policyWrap, string parsedTitle)
        {
            IEnumerable<MovieSearchResult> searchResultAll = await policyWrap.ExecuteAsync(() => _movieAPIClient.SearchMovieAsync(parsedTitle));

            // ToList is invoked to trigger execution
            IEnumerable<MovieSearchResult> searchResult = TokenizeSearchResultsAndFilter(parsedTitle, searchResultAll).ToList();

            int resultCount = searchResult.Count();
            return resultCount switch
            {
                0 => throw new NoSearchResultsError($"No search results for \"{parsedTitle}\" "),
                > 1 => throw new MultipleSearchResultsError($"Multiple search results for \"{parsedTitle}\"; count = {resultCount}"),
                _ => searchResult.First()
            };
        }

        private async Task<MovieSearchResult> SearchAndPickAsync(AsyncPolicyWrap policyWrap, string parsedTitle, string parsedReleaseDate)
        {
            var dateTol = new ReleaseDateToleranceNeighbourhood(parsedReleaseDate);

            // if release date cannot be parsed into an int then we fall back into the other overload which only uses the parsed title
            if (!dateTol.ParseSuccess) return await SearchAndPickAsync(policyWrap, parsedTitle);

            IEnumerable<MovieSearchResult> searchResultAll = await policyWrap.ExecuteAsync(
                () => _movieAPIClient.SearchMovieAsync(parsedTitle, dateTol.ReleaseDate)
            );

            // ToList is invoked to trigger execution
            IEnumerable<MovieSearchResult> searchResult = TokenizeSearchResultsAndFilter(parsedTitle, searchResultAll).ToList();

            // if no initial results are found using the original release date then we search
            // using the release dates in the tolerance neighbourhood
            if (!searchResult.Any())
            {
                Log.Debug("No results for \"{ParsedTitle}\" with release date {ReleaseDate}", parsedTitle, dateTol.ReleaseDate);
                Log.Debug("Searching the same title with other release dates: {OtherReleaseDates}", string.Join(", ", dateTol.Neighbourhood));
                
                var searchResultAllOtherDates = new List<MovieSearchResult>();
                foreach (int date in dateTol.Neighbourhood)
                {
                    searchResultAllOtherDates.AddRange(await policyWrap.ExecuteAsync(
                        () => _movieAPIClient.SearchMovieAsync(parsedTitle, date)
                    ));
                }

                // ToList is invoked to trigger execution
                searchResult = TokenizeSearchResultsAndFilter(parsedTitle, searchResultAllOtherDates).ToList();
            }

            int resultCount = searchResult.Count();
            return resultCount switch
            {
                0 => throw new NoSearchResultsError($"No search results for \"{parsedTitle}\" with release date in {dateTol}"),
                > 1 => throw new MultipleSearchResultsError($"Multiple search results for \"{parsedTitle}\"  with release date in {dateTol}; count = {resultCount}"),
                _ => searchResult.First()
            };
        }

        /// <summary>
        /// Tokenizes the search results and filters using both Title and OriginalTitle.
        /// </summary>
        private static IEnumerable<MovieSearchResult> TokenizeSearchResultsAndFilter(string parsedTitle, IEnumerable<MovieSearchResult> searchResultAll)
        {
            IEnumerable<string> titleTokens = parsedTitle.GetStringTokensWithoutPunctuation();
            return searchResultAll.Where(
                r => titleTokens.SequenceEqual(r.Title.GetStringTokensWithoutPunctuation(removeDiacritics: true))
                    || titleTokens.SequenceEqual(r.OriginalTitle.GetStringTokensWithoutPunctuation(removeDiacritics: true))
            );
        }

        /// <summary>
        /// Nested class <c>ReleaseDateToleranceNeighbourhood</c> defines the tolerance given for the release date when searching for movie info online.
        /// </summary>
        class ReleaseDateToleranceNeighbourhood
        {
            public const int NeighbourhoodRadius = 1;

            public readonly bool ParseSuccess;

            public readonly int ReleaseDate;

            /// <summary>
            /// All the integers in the closed interval [ReleaseDate - NeighbourhoodRadius, ReleaseDate + NeighbourhoodRadius], except for ReleaseDate. 
            /// </summary>
            public IEnumerable<int> Neighbourhood
            {
                get => ParseSuccess ?
                    Enumerable.Range(1, NeighbourhoodRadius).SelectMany(tol => new[] { ReleaseDate + tol, ReleaseDate - tol })
                    : Enumerable.Empty<int>();
            }

            public ReleaseDateToleranceNeighbourhood(string releaseDateString)
            {
                bool parseSucess = int.TryParse(releaseDateString, out int releaseDate);

                // from wikipedia:
                //          1888 - In Leeds, England Louis Le Prince films Roundhay Garden Scene, believed to be the first motion picture recorded.[5]
                // https://en.wikipedia.org/wiki/List_of_cinematic_firsts
                ParseSuccess = parseSucess && 1888 <= releaseDate && releaseDate <= 2100;
                if (!ParseSuccess)
                {
                    Log.Debug("Cannot parse release date string to int: \"{ReleaseDateString}\"", releaseDateString);
                    releaseDate = 0;
                }

                ReleaseDate = releaseDate;
            }

            public override string ToString() => ReleaseDate.ToString() + ", " + string.Join(", ", Neighbourhood);
        }
    }

}