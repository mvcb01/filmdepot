using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    // example of a concrete, non-generic implementation would be with TEntity -> Genre and TAPIResult -> MovieGenreResult
    public abstract class MovieDetailsFetcherAbstract<TDetailEntity, TAPIResult>
        where TDetailEntity : IExternalEntity
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

        public abstract IEnumerable<TDetailEntity> GetExistingDetailEntitiesInRepo();

        // should be asynchronous and call one the methods of IMovieAPIClient
        public abstract Task<IEnumerable<TAPIResult>> GetMovieDetailsFromApiAsync(int externalId);

        // TAPIResult to TDetailEntity conversion
        public abstract TDetailEntity CastApiResultToDetailEntity(TAPIResult apiresult);

        public abstract void AddDetailsToMovieEntity(Movie movie, IEnumerable<TDetailEntity> details);

        public async Task PopulateDetails()
        {
            IEnumerable<Movie> moviesWithoutDetails = GetMoviesWithoutDetails();
            if (!moviesWithoutDetails.Any())
            {
                return;
            }

            // maps each movie id to the task that returns its details from the movie api
            var detailTasks = new Dictionary<int, Task<IEnumerable<TAPIResult>>>();
            foreach (Movie movie in moviesWithoutDetails)
            {
                detailTasks.Add(movie.ExternalId, GetMovieDetailsFromApiAsync(movie.ExternalId));
            }

            await Task.WhenAll(detailTasks.Values);

            // gets the IEnumerable<TAPIResult> of each task, flattens to a single IEnumerable<TAPIResult> and
            // gets the distinct results using the ExternalId property
            IEnumerable<TAPIResult> distinctDetailResults = detailTasks.Values
                .SelectMany(t => t.Result)
                .GroupBy(res => res.ExternalId)
                .Select(group => group.First());

            IEnumerable<TDetailEntity> existingDetailEntities = GetExistingDetailEntitiesInRepo();
            IEnumerable<int> existingDetailEntitiesExtIds = existingDetailEntities.Select(d => d.ExternalId);

            // new detail entities: these are still not part of its repo;
            // the ToList method is called to force execution and make sure such entities are
            // created exactly once;
            IEnumerable<TDetailEntity> newDetailEntities = distinctDetailResults
                .Where(res => !existingDetailEntitiesExtIds.Contains(res.ExternalId))
                .Select(res => CastApiResultToDetailEntity(res))
                .ToList();

            // gets the new details for each movie
            foreach (Movie movie in moviesWithoutDetails)
            {
                IEnumerable<int> detailExternalIdsForMovie = detailTasks[movie.ExternalId].Result.Select(res => res.ExternalId);
                IEnumerable<TDetailEntity> movieDetailsInRepo = existingDetailEntities.Where(d => detailExternalIdsForMovie.Contains(d.ExternalId));
                IEnumerable<TDetailEntity> movieDetailsNew = newDetailEntities.Where(d => detailExternalIdsForMovie.Contains(d.ExternalId));
                AddDetailsToMovieEntity(movie, movieDetailsInRepo.Concat(movieDetailsNew));
            }

            this._unitOfWork.Complete();
        }

    }
}