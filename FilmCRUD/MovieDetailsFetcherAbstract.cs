using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System;
using Polly;
using Polly.RateLimit;
using Polly.Wrap;
using Serilog;
using ConfigUtils.Interfaces;
using FilmCRUD.Interfaces;
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

        public static string DetailType {  get => typeof(TDetailEntity).Name; }

        protected IUnitOfWork _unitOfWork { get; init; }

        protected IMovieAPIClient _movieAPIClient { get; init; }

        private readonly ILogger _fetchingErrorsLogger;

        private IFileSystemIOWrapper _fileSystemIOWrapper { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        public MovieDetailsFetcherAbstract(
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

        public MovieDetailsFetcherAbstract(
            IUnitOfWork unitOfWork,
            IFileSystemIOWrapper fileSystemIOWrapper,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient,
            ILogger fetchingErrorsLogger) : this(unitOfWork, fileSystemIOWrapper, appSettingsManager, movieAPIClient) => this._fetchingErrorsLogger = fetchingErrorsLogger;

        public abstract IEnumerable<Movie> GetMoviesWithoutDetails();

        public abstract IEnumerable<TDetailEntity> GetExistingDetailEntitiesInRepo();

        // should be asynchronous and call one of the methods of IMovieAPIClient
        public abstract Task<IEnumerable<TAPIResult>> GetMovieDetailsFromApiAsync(int externalId);

        // TAPIResult to TDetailEntity conversion
        public abstract TDetailEntity CastApiResultToDetailEntity(TAPIResult apiresult);

        public abstract void AddDetailsToMovieEntity(Movie movie, IEnumerable<TDetailEntity> details);

        public async Task PopulateDetails()
        {
            IEnumerable<Movie> moviesWithoutDetails = GetMoviesWithoutDetails();

            int moviesWithoutDetailsCount = moviesWithoutDetails.Count();

            if (moviesWithoutDetailsCount == 0)
            {
                Log.Information("No movies without details for detail type {DetailType}", DetailType);
                return;
            }
            else
            {
                Log.Information("Movies without details for detail type {DetailType} - total count: {TotalCount}", DetailType, moviesWithoutDetailsCount);
            }

            // notice the order of the async policies when calling Policy.WrapAsync:
            //      outermost (at left) to innermost (at right)
            IRateLimitPolicyConfig rateLimitConfig = this._appSettingsManager.GetRateLimitPolicyConfig();
            AsyncPolicyWrap policyWrap = Policy.WrapAsync(
                APIClientPolicyBuilder.GetAsyncRetryPolicy<RateLimitRejectedException>(this._appSettingsManager.GetRetryPolicyConfig()),
                APIClientPolicyBuilder.GetAsyncRateLimitPolicy(rateLimitConfig)
            );

            // letting the token bucket fill for the current timespan...
            await Task.Delay(rateLimitConfig.PerTimeSpan);

            IEnumerable<TDetailEntity> detailEntitiesInRepo = GetExistingDetailEntitiesInRepo();
            IEnumerable<int> detailEntitiesInRepoExtIds = detailEntitiesInRepo.Select(e => e.ExternalId);
            var newDetailEntities = new List<TDetailEntity>();

            // to map Movie.ExternalId -> { TDetailEntity_0.ExternalId, TDetailEntity_1.ExternalId, ... }
            var movieToDetailsMapping = new Dictionary<int, IEnumerable<int>>();

            var errors = new List<string>();

            // TODO: investigate how to properly use the limit+retry policy with Task.WhenAll...
            foreach (Movie movie in moviesWithoutDetails)
            {
                try
                {
                    IEnumerable<TAPIResult> details = await policyWrap.ExecuteAsync(() => GetMovieDetailsFromApiAsync(movie.ExternalId));

                    // saving new TDetailEntity objects
                    IEnumerable<TAPIResult> detailsNew = details.Where(
                        res => !detailEntitiesInRepoExtIds.Contains(res.ExternalId) 
                               && !newDetailEntities.Select(d => d.ExternalId).Contains(res.ExternalId));
                    newDetailEntities.AddRange(detailsNew.Select(d => CastApiResultToDetailEntity(d)));

                    // saving details for this movie entity
                    movieToDetailsMapping.Add(movie.ExternalId, details.Select(res => res.ExternalId));
                }
                // in case we exceed IRetryPolicyConfig.RetryCount; no need to throw again, just let the others run
                catch (RateLimitRejectedException ex)
                {
                    errors.Add($"Rate Limit error for {movie.Title} ({movie.ReleaseDate}); Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                // invalid external ids should not stop execution
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    errors.Add($"Invalid external id for {movie.Title} ({movie.ReleaseDate}): {movie.ExternalId}");
                }
            }

            // all the details for each Movie entity are now known - both new detail entitites and previously existing ones - without duplicates
            // only thing left to do is assigning those details
            foreach (Movie movie in moviesWithoutDetails)
            {
                IEnumerable<int> detailIds = movieToDetailsMapping[movie.ExternalId];
                IEnumerable<TDetailEntity> movieDetailsInRepo = detailEntitiesInRepo.Where(e => detailIds.Contains(e.ExternalId));
                IEnumerable<TDetailEntity> movieDetailsNew = newDetailEntities.Where(e => detailIds.Contains(e.ExternalId));
                AddDetailsToMovieEntity(movie, movieDetailsInRepo.Concat(movieDetailsNew));
            }

            string dtNow = DateTime.Now.ToString("yyyyMMddHHmmss");
            string detailClassName = typeof(TDetailEntity).Name;
            PersistErrorInfo($"details_fetcher_errors_{detailClassName}_{dtNow}.txt", errors);

            this._unitOfWork.Complete();
        }

        private void PersistErrorInfo(string filename, IEnumerable<string> errors)
        {
            if (!errors.Any()) return;

            string errorsFpath = Path.Combine(this._appSettingsManager.GetWarehouseContentsTextFilesDirectory(), filename);
            System.Console.WriteLine($"Errors fetching details; see {errorsFpath}");
            this._fileSystemIOWrapper.WriteAllText(errorsFpath, string.Join("\n\n", errors));
        }

    }

}