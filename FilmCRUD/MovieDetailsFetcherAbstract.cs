using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public abstract class MovieDetailsFetcherAbstract<TEntity, TAPIResult>
        where TEntity : IExternalEntity
        where TAPIResult : IExternalEntity
    {
        protected IUnitOfWork _unitOfWork { get; init; }

        protected IMovieAPIClient _movieAPIClient { get; init; }

        public MovieDetailsFetcherAbstract(IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
            this._movieAPIClient = movieAPIClient;
        }

        public abstract IEnumerable<Movie> GetMoviesWithoutDetails();

        public abstract IEnumerable<TEntity> GetExistingEntitiesInRepo();

        // should be asynchronous and call one the methods of IMovieAPIClient
        public abstract Task<IEnumerable<TAPIResult>> GetMovieDetailsFromApiAsync(int externalId);

        public void PopulateDetails()
        {

        }

    }
}