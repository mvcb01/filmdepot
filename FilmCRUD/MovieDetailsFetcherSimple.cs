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

            Dictionary<int, IEnumerable<string>> kwds = await GetDetailsSimple<IEnumerable<string>>(moviesWithoutKeywords, this._movieAPIClient.GetMovieKeywordsAsync, "details_fetcher_errors_KeyWords");

            try
            {
                foreach (Movie movie in moviesWithoutKeywords)
                {
                    if (!kwds.ContainsKey(movie.ExternalId)) continue;
                    movie.Keywords = kwds[movie.ExternalId].ToList();
                }
            }
            catch (Exception)
            {
                this._unitOfWork.Dispose();
                throw;
            }

            this._unitOfWork.Complete();
        }

        public async Task PopulateMovieIMDBIds()
        {
            IEnumerable<Movie> moviesWithoutIMDBId = this._unitOfWork.Movies.GetMoviesWithoutImdbId();

            Dictionary<int, string> imdbIds = await GetDetailsSimple<string>(moviesWithoutIMDBId, this._movieAPIClient.GetMovieIMDBIdAsync, "details_fetcher_errors_IMDBIds");

            try
            {
                foreach (Movie movie in moviesWithoutIMDBId)
                {
                    if (!imdbIds.ContainsKey(movie.ExternalId)) continue;
                    movie.IMDBId = imdbIds[movie.ExternalId];
                }
            }
            catch (Exception)
            {
                this._unitOfWork.Dispose();
                throw;
            }

            this._unitOfWork.Complete();
        }

        private async Task<Dictionary<int, TDetail>> GetDetailsSimple<TDetail>(IEnumerable<Movie> movies, Func<int, Task<TDetail>> detailsFunc, string errorsFileName)
        {
            // notice the order of the async policies when calling Policy.WrapAsync:
            //      outermost (at left) to innermost (at right)
            IRateLimitPolicyConfig rateLimitConfig = this._appSettingsManager.GetRateLimitPolicyConfig();
            AsyncPolicyWrap policyWrap = Policy.WrapAsync(
                APIClientPolicyBuilder.GetAsyncRetryPolicy<RateLimitRejectedException>(this._appSettingsManager.GetRetryPolicyConfig()),
                APIClientPolicyBuilder.GetAsyncRateLimitPolicy(rateLimitConfig));

            await Task.Delay(rateLimitConfig.PerTimeSpan);

            var detailsDict = new Dictionary<int, TDetail>();

            var errors = new List<string>();

            foreach (Movie movie in movies)
            {
                try
                {
                    TDetail detail = await policyWrap.ExecuteAsync(() => detailsFunc(movie.ExternalId));
                    detailsDict.Add(movie.ExternalId, detail);  
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
            PersistErrorInfo($"{errorsFileName}_{dtNow}.txt", errors);

            return detailsDict;
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