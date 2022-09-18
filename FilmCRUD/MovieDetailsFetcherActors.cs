using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigUtils.Interfaces;
using FilmCRUD.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class MovieDetailsFetcherActors : MovieDetailsFetcherAbstract<Actor, MovieActorResult>
    {
        public MovieDetailsFetcherActors(
            IUnitOfWork unitOfWork,
            IFileSystemIOWrapper fileSystemIOWrapper,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient)
            : base(unitOfWork, fileSystemIOWrapper, appSettingsManager, movieAPIClient) { }

        public override IEnumerable<Actor> GetExistingDetailEntitiesInRepo() => this._unitOfWork.Actors.GetAll();

        public override async Task<IEnumerable<MovieActorResult>> GetMovieDetailsFromApiAsync(int externalId)
        {
            return await this._movieAPIClient.GetMovieActorsAsync(externalId);
        }

        public override IEnumerable<Movie> GetMoviesWithoutDetails() => this._unitOfWork.Movies.GetMoviesWithoutActors();

        // explicit cast is defined in MovieActorResult
        public override Actor CastApiResultToDetailEntity(MovieActorResult apiresult) => (Actor)apiresult;

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