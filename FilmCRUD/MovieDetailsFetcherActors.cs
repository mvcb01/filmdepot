using System.Collections.Generic;
using System.Threading.Tasks;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class MovieDetailsFetcherActors : MovieDetailsFetcherAbstract<Actor, MovieActorResult>
    {
        public MovieDetailsFetcherActors(IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient) : base(unitOfWork, movieAPIClient)
        {
        }

        public override IEnumerable<Actor> GetExistingDetailEntitiesInRepo()
        {
            return this._unitOfWork.Actors.GetAll();
        }

        public override async Task<IEnumerable<MovieActorResult>> GetMovieDetailsFromApiAsync(int externalId)
        {
            return await this._movieAPIClient.GetMovieActorsAsync(externalId);
        }

        public override IEnumerable<Movie> GetMoviesWithoutDetails()
        {
            return this._unitOfWork.Movies.GetMoviesWithoutActors();
        }

        public override Actor CastApiResultToDetailEntity(MovieActorResult apiresult)
        {
            // explicit cast is defined in MovieActorResult
            return (Actor)apiresult;
        }

        public override void AddDetailsToMovieEntity(Movie movie, IEnumerable<Actor> details)
        {
            // ICollection does not necessarily have the AddRange method
            foreach (var actor in details)
            {
                movie.Actors.Add(actor);
            }
        }

    }
}