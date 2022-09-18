using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;
using ConfigUtils.Interfaces;
using FilmCRUD.Interfaces;

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
    }

}