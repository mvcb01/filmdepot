using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;

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

        private IMovieAPIClient _movieAPIClient { get; init; }

        public MovieDetailsFetcherSimple(IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
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
        }
    }

}