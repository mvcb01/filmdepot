using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;
using ConfigUtils.Interfaces;
using FilmCRUD.Interfaces;
using FilmCRUD.Helpers;
using Polly.RateLimit;
using Polly.Wrap;
using Polly;
using System;
using System.Net.Http;
using System.Net;
using System.Collections;
using System.IO;

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

        public async Task PopulateMovieKeywords()
        {
            IEnumerable<Movie> moviesWithoutKeywords = this._unitOfWork.Movies.GetMoviesWithoutKeywords();

            AsyncPolicyWrap policyWrap = GetPolicyWrapFromConfigs(out TimeSpan initialDelay);

            await Task.Delay(initialDelay);

            var errors = new List<string>();

            foreach (Movie movie in moviesWithoutKeywords)
            {
                try
                {
                    IEnumerable<string> movieKwds = await policyWrap.ExecuteAsync(() => this._movieAPIClient.GetMovieKeywordsAsync(movie.ExternalId));
                    movie.Keywords = movieKwds.ToList();
                }
                catch (RateLimitRejectedException ex)
                {
                    errors.Add($"Rate Limit error for {movie.Title} ({movie.ReleaseDate}); Retry after milliseconds: {ex.RetryAfter.TotalMilliseconds}; Message: {ex.Message}");
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    errors.Add($"Invalid external id for {movie.Title} ({movie.ReleaseDate}): {movie.ExternalId}");
                }
                catch (Exception)
                {
                    this._unitOfWork.Dispose();
                    throw;
                }
            }

            string dtNow = DateTime.Now.ToString("yyyyMMddHHmmss");
            PersistErrorInfo($"details_fetcher_errors_KeyWords_{dtNow}.txt", errors);
            
            this._unitOfWork.Complete();
        }

        public async Task PopulateMovieKeywords_OLD()
        {
            IEnumerable<Movie> moviesWithoutKeywords = this._unitOfWork.Movies.GetMoviesWithoutKeywords();

            var keywordTasks = new Dictionary<int, Task<IEnumerable<string>>>();
            foreach (var movie in moviesWithoutKeywords)
            {
                keywordTasks.Add(movie.ExternalId, this._movieAPIClient.GetMovieKeywordsAsync(movie.ExternalId));
            }

            await Task.WhenAll(keywordTasks.Values);

            foreach (var movie in moviesWithoutKeywords)
            {
                movie.Keywords = keywordTasks[movie.ExternalId].Result.ToList();
            }

            this._unitOfWork.Complete();
        }

        public async Task PopulateMovieIMDBIds()
        {
            IEnumerable<Movie> moviesWithoutIMDBId = this._unitOfWork.Movies.GetMoviesWithoutImdbId();

            var IMDBIdTasks = new Dictionary<int, Task<string>>();

            foreach (var movie in moviesWithoutIMDBId)
            {
                IMDBIdTasks.Add(movie.ExternalId, this._movieAPIClient.GetMovieIMDBIdAsync(movie.ExternalId));
            }

            await Task.WhenAll(IMDBIdTasks.Values);

            foreach (var movie in moviesWithoutIMDBId)
            {
                movie.IMDBId = IMDBIdTasks[movie.ExternalId].Result;
            }

            this._unitOfWork.Complete();
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
            if (!errors.Any()) return;

            string errorsFpath = Path.Combine(this._appSettingsManager.GetWarehouseContentsTextFilesDirectory(), filename);
            System.Console.WriteLine($"Errors fetching details; see {errorsFpath}");
            this._fileSystemIOWrapper.WriteAllText(errorsFpath, string.Join("\n\n", errors));
        }
    }

}