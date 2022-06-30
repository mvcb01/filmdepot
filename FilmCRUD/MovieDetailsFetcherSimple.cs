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
    }

}