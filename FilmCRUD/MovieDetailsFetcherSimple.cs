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

        private IAppSettingsManager _appSettingsManager { get; init; }

        private IMovieAPIClient _movieAPIClient { get; init; }

        private readonly ILogger _fetchingErrorsLogger;

        public MovieDetailsFetcherSimple(
            IUnitOfWork unitOfWork,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
            this._appSettingsManager = appSettingsManager;
            this._movieAPIClient = movieAPIClient;
        }

        public MovieDetailsFetcherSimple(
            IUnitOfWork unitOfWork,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient,
            ILogger fetchingErrorsLogger) : this(unitOfWork, appSettingsManager, movieAPIClient) => this._fetchingErrorsLogger = fetchingErrorsLogger;

        public async Task PopulateMovieKeywords(int maxApiCalls = -1)
        {
            Func<IEnumerable<Movie>> moviesWithoutDetailsGetterFunc = this._unitOfWork.Movies.GetMoviesWithoutKeywords;

            Func<int, Task<IEnumerable<string>>> detailsGetterFunc = this._movieAPIClient.GetMovieKeywordsAsync;

            Action<Movie, IEnumerable<string>> detailsPopulatorAction = (movie, kwds) => movie.Keywords = kwds.ToList();

            await PopulateDetailsSimpleAsync<IEnumerable<string>>(moviesWithoutDetailsGetterFunc, detailsGetterFunc, detailsPopulatorAction, "Keywords");
        }

        public async Task PopulateMovieIMDBIds()
        {
            Func<IEnumerable<Movie>> moviesWithoutDetailsGetterFunc = this._unitOfWork.Movies.GetMoviesWithoutImdbId;

            Func<int, Task<string>> detailsGetterFunc = this._movieAPIClient.GetMovieIMDBIdAsync;

            Action<Movie, string> detailsPopulatorAction = (movie, imdbId) => movie.IMDBId = imdbId;

            await PopulateDetailsSimpleAsync<string>(moviesWithoutDetailsGetterFunc, detailsGetterFunc, detailsPopulatorAction, "IMDBId");
        }

        private async Task PopulateDetailsSimpleAsync<TDetail>(
            Func<IEnumerable<Movie>> moviesWithoutDetailsGetterFunc,
            Func<int, Task<TDetail>> detailsGetterFunc,
            Action<Movie, TDetail> detailsPopulatorAction,
            string detailType)
        {
            IEnumerable<Movie> moviesWithoutDetails = moviesWithoutDetailsGetterFunc();

            int moviesWithoutDetailsCount = moviesWithoutDetails.Count();

            Log.Information("Movies without details for detail type {DetailType} - total count: {TotalCount}", detailType, moviesWithoutDetailsCount);

            if (moviesWithoutDetailsCount == 0) return;

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            var detailsDict = new Dictionary<int, TDetail>();

            var errors = new List<string>();

            var logStep = (int)Math.Ceiling((decimal)moviesWithoutDetailsCount / 20.0m);

            Log.Information("Finding movies details for detail type {DetailType}...", detailType);
            Log.Information("API base address: {ApiBaseAddress}", this._movieAPIClient.ApiBaseAddress);
            foreach (var (movie, idx) in moviesWithoutDetails.Select((value, idx) => (value, idx + 1)))
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

            Log.Information("Assigning new {DetailType} details to movie entities...", detailType);

            try
            {
                foreach (var (movie, idx) in moviesWithoutDetails.Select((value, idx) => (value, idx + 1)))
                {
                    if (!detailsDict.ContainsKey(movie.ExternalId))
                    {
                        Log.Debug("Skipping movie with ExternalId = {ExternalId}...", movie.ExternalId);
                        continue;
                    }

                    detailsPopulatorAction(movie, detailsDict[movie.ExternalId]);

                    if (idx % logStep == 0 || idx == moviesWithoutDetailsCount)
                    {
                        Log.Information("Assigning new {DetailType} details to movie entities - {Index} out of {Total}", detailType, idx, moviesWithoutDetailsCount);
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

            Log.Information("------- FETCH DETAILS SUMMARY: {DetailType} -------", detailType);
            Log.Information("Searched movie count: {MovieCount}", moviesWithoutDetailsCount);
            Log.Information("Error count: {ErrorCount}", errorCount);
            Log.Information("------------------------------------------------");

            this._unitOfWork.Complete();
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