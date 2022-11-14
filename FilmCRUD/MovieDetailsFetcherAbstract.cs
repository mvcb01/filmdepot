using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System;
using Polly;
using Polly.RateLimit;
using Polly.Wrap;
using Serilog;
using ConfigUtils.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;
using FilmCRUD.Helpers;

namespace FilmCRUD
{
    /// <summary>
    /// Class <c>MovieDetailsFetcherAbstract</c> To populate Movie properties that have their specific return type
    /// in IMovieAPIClient. Example of a concrete, non-generic implementation would be where TEntity is Genre
    /// and TAPIResult is MovieGenreResult.
    /// </summary>
    /// <typeparam name="TDetailEntity">The Movie property type.</typeparam>
    /// <typeparam name="TAPIResult">The return type of IMovieAPIClient for the relevant method.</typeparam>
    public abstract class MovieDetailsFetcherAbstract<TDetailEntity, TAPIResult>
        where TDetailEntity : IExternalEntity
        where TAPIResult : IExternalEntity
    {
        public static string DetailType { get => typeof(TDetailEntity).Name; }

        protected IUnitOfWork _unitOfWork { get; init; }

        protected IMovieAPIClient _movieAPIClient { get; init; }

        private readonly ILogger _fetchingErrorsLogger;

        private IAppSettingsManager _appSettingsManager { get; init; }

        public MovieDetailsFetcherAbstract(
            IUnitOfWork unitOfWork,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
            this._appSettingsManager = appSettingsManager;
            this._movieAPIClient = movieAPIClient;
        }

        public MovieDetailsFetcherAbstract(
            IUnitOfWork unitOfWork,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient,
            ILogger fetchingErrorsLogger) : this(unitOfWork, appSettingsManager, movieAPIClient) => this._fetchingErrorsLogger = fetchingErrorsLogger;

        public abstract IEnumerable<Movie> GetMoviesWithoutDetails();

        public abstract IEnumerable<TDetailEntity> GetExistingDetailEntitiesInRepo();

        // should be asynchronous and call one of the methods of IMovieAPIClient;
        // each concrete subclass should invoke the relevant method, e.g., GetMovieActorsAsync, GetMovieGenresAsync etc...
        public abstract Task<IEnumerable<TAPIResult>> GetMovieDetailsFromApiAsync(int externalId);

        // TAPIResult to TDetailEntity conversion
        public abstract TDetailEntity CastApiResultToDetailEntity(TAPIResult apiresult);

        public abstract void AddDetailsToMovieEntity(Movie movie, IEnumerable<TDetailEntity> details);

        public async Task PopulateDetails(int maxApiCalls = -1)
        {
            IEnumerable<Movie> moviesWithoutDetails = GetMoviesWithoutDetails();

            int moviesWithoutDetailsCount = moviesWithoutDetails.Count();

            Log.Information("Movies without details for detail type {DetailType} - total count: {TotalCount}", DetailType, moviesWithoutDetailsCount);

            if (moviesWithoutDetailsCount == 0) return;

            if (0 < maxApiCalls && maxApiCalls < moviesWithoutDetailsCount)
            {
                Log.Information("Limiting number of API calls to {CallLimit}", maxApiCalls);
                moviesWithoutDetails = moviesWithoutDetails.Take(maxApiCalls).ToList();
                moviesWithoutDetailsCount = maxApiCalls;
            }

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            // letting the token bucket fill for the current timespan...
            await Task.Delay(initialDelay);

            // existing Genres, Directors etc... in repo
            IEnumerable<TDetailEntity> detailEntitiesInRepo = GetExistingDetailEntitiesInRepo();
            IEnumerable<int> detailEntitiesInRepoExtIds = detailEntitiesInRepo.Select(e => e.ExternalId);

            // to save previously unknown Genres, Directors etc...
            var newDetailEntities = new List<TDetailEntity>();

            // to map each Movie.ExternalId to its details:
            //      Movie.ExternalId ---> { TDetailEntity_0.ExternalId, TDetailEntity_1.ExternalId, ... }
            var movieToDetailsMapping = new Dictionary<int, IEnumerable<int>>();

            var errors = new List<string>();

            var logStep = (int)Math.Ceiling((decimal)moviesWithoutDetailsCount / 20.0m);

            Log.Information("Finding movies details for detail type {DetailType}...", DetailType);
            Log.Information("API base address: {ApiBaseAddress}", this._movieAPIClient.ApiBaseAddress);
            // TODO: investigate how to properly use the limit+retry policy with Task.WhenAll...
            foreach (var (movie, idx) in moviesWithoutDetails.Select((value, idx) => (value, idx + 1)))
            {
                try
                {
                    IEnumerable<TAPIResult> details = await policyWrap.ExecuteAsync(() => GetMovieDetailsFromApiAsync(movie.ExternalId));

                    // saving new TDetailEntity objects in variable newDetailEntities;
                    // each new TDetailEntity object may will satisfy exactly one of the following:
                    //      - was already in the repo, meaning it's in variable detailEntitiesInRepo
                    //      - was not in repo but it is already known from an earlier iteration, meaning it's already in variable newDetailEntities
                    //      - was not already in repo and also not in variable newDetailEntities
                    IEnumerable<TAPIResult> detailsNew = details.Where(
                        res => !detailEntitiesInRepoExtIds.Contains(res.ExternalId) 
                               && !newDetailEntities.Select(d => d.ExternalId).Contains(res.ExternalId));
                    newDetailEntities.AddRange(detailsNew.Select(d => CastApiResultToDetailEntity(d)));

                    // saving detail ids for this movie entity
                    movieToDetailsMapping.Add(movie.ExternalId, details.Select(res => res.ExternalId));

                    Log.Debug("FOUND: {Movie} - {DetailType} count: {Count}", movie.ToString(), DetailType, details.Count());
                }
                // in case we exceed IRetryPolicyConfig.RetryCount; no need to throw again, just let the others run
                catch (RateLimitRejectedException ex)
                {
                    Log.Debug("RATE LIMIT ERROR: {Movie}", movie.ToString());
                    errors.Add($"RATE LIMIT ERROR: {movie.Title} ({movie.ReleaseDate}); Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                // invalid external ids should not stop execution
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Debug("INVALID EXTERNAL ID: {Movie} - {ExternalId}", movie.ToString(), movie.ExternalId);
                    errors.Add($"INVALID EXTERNAL ID: {movie} - {movie.ExternalId}");
                }

                // let it fail in case any other exception occurrs: DbContext was not accessed so no need to dispose

                if (idx % logStep == 0 || idx == moviesWithoutDetailsCount)
                {
                    Log.Information("Finding movies details for detail type {DetailType} - {Index} out of {Total}", DetailType, idx, moviesWithoutDetailsCount);
                }
            }

            // all the details for each Movie entity are now known - both new detail entitites and previously existing ones - without duplicates;
            // only thing left to do is to assign those details to each movie
            Log.Information("Assigning new {DetailType} details to movie entities...", DetailType);
            try
            {
                foreach (var (movie, idx) in moviesWithoutDetails.Select((value, idx) => (value, idx + 1)))
                {
                    // skip if there was an error fetching details for this external id
                    if (!movieToDetailsMapping.ContainsKey(movie.ExternalId))
                    {
                        Log.Debug("Skipping movie with ExternalId = {ExternalId}...", movie.ExternalId);
                        continue;
                    }

                    IEnumerable<int> movieDetailIds = movieToDetailsMapping[movie.ExternalId];

                    // filtering detailEntitiesInRepo and newDetailEntities for the details of this movie;
                    // basically a left-semi join
                    IEnumerable<TDetailEntity> movieDetailsInRepo = detailEntitiesInRepo.Join(
                        movieDetailIds,
                        detailEntity => detailEntity.ExternalId,
                        detailId => detailId,
                        (detailEntity, detailId) => detailEntity);
                    IEnumerable<TDetailEntity> movieDetailsNew = newDetailEntities.Join(
                        movieDetailIds,
                        detailEntity => detailEntity.ExternalId,
                        detailId => detailId,
                        (detailEntity, detailId) => detailEntity);

                    // example: movie.Genres.Add(genre) for each genre in movieDetailsNew
                    IEnumerable<TDetailEntity> allMovieDetails = movieDetailsInRepo.Concat(movieDetailsNew);
                    AddDetailsToMovieEntity(movie, allMovieDetails);

                    if (idx % logStep == 0 || idx == moviesWithoutDetailsCount)
                    {
                        Log.Information("Assigning new {DetailType} details to movie entities - {Index} out of {Total}", DetailType, idx, moviesWithoutDetailsCount);
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

            Log.Information("------- FETCH DETAILS SUMMARY: {DetailType} -------", DetailType);
            Log.Information("Searched movie count: {MovieCount}", moviesWithoutDetailsCount);
            Log.Information("New detail entities found: {NewDetailCount}", newDetailEntities.Count());
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