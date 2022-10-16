using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Polly.RateLimit;
using Polly.Wrap;
using Polly;
using System;
using System.Net.Http;
using System.Net;
using System.IO;
using Serilog;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;
using ConfigUtils.Interfaces;
using FilmCRUD.Interfaces;
using FilmCRUD.Helpers;
using System.Data;

namespace FilmCRUD
{
    /// <summary>
    /// Class <c>MovieDetailsFetcherSimple</c> To populate Movie properties that do not have a custom return type
    /// on the relevant method of IMovieAPIClient.
    /// Example: the Keywords movie property, which is a IEnumerable<string> found by the
    /// method IMovieAPIClient.GetMovieKeywordsAsync
    /// </summary>
    public class MovieDetailsFetcherSimple
    {
        private IUnitOfWork _unitOfWork { get; init; }

        private IFileSystemIOWrapper _fileSystemIOWrapper { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        private IMovieAPIClient _movieAPIClient { get; init; }

        private readonly ILogger _fetchingErrorsLogger;

        public MovieDetailsFetcherSimple(
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

        public MovieDetailsFetcherSimple(
            IUnitOfWork unitOfWork,
            IFileSystemIOWrapper fileSystemIOWrapper,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient,
            ILogger fetchingErrorsLogger) : this(unitOfWork, fileSystemIOWrapper, appSettingsManager, movieAPIClient) => this._fetchingErrorsLogger = fetchingErrorsLogger;

        public async Task PopulateMovieKeywords()
        {
            IEnumerable<Movie> moviesWithoutKeywords = this._unitOfWork.Movies.GetMoviesWithoutKeywords();

            var (kwds, errors) = await GetDetailsSimple<IEnumerable<string>>(moviesWithoutKeywords, this._movieAPIClient.GetMovieKeywordsAsync, "Keywords");

            int moviesWithoutKeywordsCount = moviesWithoutKeywords.Count();
            var logStep = (int)Math.Ceiling((decimal)moviesWithoutKeywordsCount / 20.0m);

            Log.Information("Assigning new {DetailType} details to movie entities...", "Keywords");
            try
            {
                foreach (var (movie, idx) in moviesWithoutKeywords.Select((value, idx) => (value, idx + 1)))
                {
                    if (!kwds.ContainsKey(movie.ExternalId))
                    {
                        Log.Debug("Skipping movie with ExternalId = {ExternalId}...", movie.ExternalId);
                        continue;
                    }
                    movie.Keywords = kwds[movie.ExternalId].ToList();

                    if (idx % logStep == 0 || idx == moviesWithoutKeywordsCount)
                    {
                        Log.Information("Assigning new {DetailType} details to movie entities - {Index} out of {Total}", "Keywords", idx, moviesWithoutKeywordsCount);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.Message);
                this._unitOfWork.Dispose();
                throw;
            }

            int errorCount = errors.Count();
            if (errorCount > 0)
            {
                Log.Information("Saving feching errors in separate file...");
                this._fetchingErrorsLogger?.Information("----------------------------------");
                this._fetchingErrorsLogger?.Information("--- {DateTime} ---", DateTime.Now.ToString("g"));
                this._fetchingErrorsLogger?.Information("----------------------------------");
                errors.ForEach(e => this._fetchingErrorsLogger?.Error(e));
            }

            Log.Information("------- FETCH DETAILS SUMMARY: {DetailType} -------", "Keywords");
            Log.Information("Searched movie count: {MovieCount}", moviesWithoutKeywordsCount);
            Log.Information("Error count: {ErrorCount}", errorCount);
            Log.Information("------------------------------------------------");

            this._unitOfWork.Complete();
        }

        public async Task PopulateMovieIMDBIds()
        {
            IEnumerable<Movie> moviesWithoutIMDBId = this._unitOfWork.Movies.GetMoviesWithoutImdbId();

            var (imdbIds, errors) = await GetDetailsSimple<string>(moviesWithoutIMDBId, this._movieAPIClient.GetMovieIMDBIdAsync, "IMDBIds");

            int moviesWithoutIMDBIdCount = moviesWithoutIMDBId.Count();
            var logStep = (int)Math.Ceiling((decimal)moviesWithoutIMDBIdCount / 20.0m);

            Log.Information("Assigning new {DetailType} details to movie entities...", "IMDBId");
            try
            {
                foreach (var (movie, idx) in moviesWithoutIMDBId.Select((value, idx) => (value, idx + 1)))
                {
                    if (!imdbIds.ContainsKey(movie.ExternalId))
                    {
                        Log.Debug("Skipping movie with ExternalId = {ExternalId}...", movie.ExternalId);
                        continue;
                    }

                    movie.IMDBId = imdbIds[movie.ExternalId];

                    if (idx % logStep == 0 || idx == moviesWithoutIMDBIdCount)
                    {
                        Log.Information("Assigning new {DetailType} details to movie entities - {Index} out of {Total}", "IMDBId", idx, moviesWithoutIMDBIdCount);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.Message);
                this._unitOfWork.Dispose();
                throw;
            }

            int errorCount = errors.Count();
            if (errorCount > 0)
            {
                Log.Information("Saving feching errors in separate file...");
                this._fetchingErrorsLogger?.Information("----------------------------------");
                this._fetchingErrorsLogger?.Information("--- {DateTime} ---", DateTime.Now.ToString("g"));
                this._fetchingErrorsLogger?.Information("----------------------------------");
                errors.ForEach(e => this._fetchingErrorsLogger?.Error(e));
            }

            Log.Information("------- FETCH DETAILS SUMMARY: {DetailType} -------", "IMDBId");
            Log.Information("Searched movie count: {MovieCount}", moviesWithoutIMDBIdCount);
            Log.Information("Error count: {ErrorCount}", errorCount);
            Log.Information("------------------------------------------------");

            this._unitOfWork.Complete();
        }

        private async Task<(Dictionary<int, TDetail> DetailsDict, List<string> errors)> GetDetailsSimple<TDetail>(
            IEnumerable<Movie> movies,
            Func<int, Task<TDetail>> detailsGetterFunc,
            string detailType)
        {
            int moviesWithoutDetailsCount = movies.Count();

            Log.Information("Movies without details for detail type {DetailType} - total count: {TotalCount}", detailType, moviesWithoutDetailsCount);

            if (moviesWithoutDetailsCount == 0) return (new Dictionary<int, TDetail>(), new List<string>());

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            var detailsDict = new Dictionary<int, TDetail>();

            var errors = new List<string>();

            var logStep = (int)Math.Ceiling((decimal)moviesWithoutDetailsCount / 20.0m);

            Log.Information("Finding movies details for detail type {DetailType}...", detailType);
            Log.Information("API base address: {ApiBaseAddress}", this._movieAPIClient.ApiBaseAddress);
            foreach (var (movie, idx) in movies.Select((value, idx) => (value, idx + 1)))
            {
                try
                {
                    TDetail detail = await policyWrap.ExecuteAsync(() => detailsGetterFunc(movie.ExternalId));
                    detailsDict.Add(movie.ExternalId, detail);

                    Log.Debug("FOUND: {Movie}", movie.ToString());
                }
                catch (RateLimitRejectedException ex)
                {
                    Log.Debug("RATE LIMIT ERROR: {Movie}", movie.ToString());
                    errors.Add($"RATE LIMIT ERROR: {movie.Title} ({movie.ReleaseDate}); Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Debug("INVALID EXTERNAL ID: {Movie} - {ExternalId}", movie.ToString(), movie.ExternalId);
                    errors.Add($"INVALID EXTERNAL ID: {movie.Title} ({movie.ReleaseDate}): {movie.ExternalId}");
                }

                // let it fail in case any other exception occurrs: DbContext was not accessed so no need to dispose

                if (idx % logStep == 0 || idx == moviesWithoutDetailsCount)
                {
                    Log.Information("Finding movies details for detail type {DetailType} - {Index} out of {Total}", detailType, idx, moviesWithoutDetailsCount);
                }
            }

            return (detailsDict, errors);
        }

        // TODO same as in class RipToMovieLinker, move somewhere else
        private AsyncPolicyWrap GetPolicyWrapFromConfigs(out TimeSpan initialDelay)
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

    }

}