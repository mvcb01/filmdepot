using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigUtils.Interfaces;
using Serilog;
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
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient)
            : base(unitOfWork, appSettingsManager, movieAPIClient) { }

        public MovieDetailsFetcherActors(
            IUnitOfWork unitOfWork,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient,
            ILogger fetchingErrorsLogger)
            : base(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger) { }

        public override IEnumerable<Actor> GetExistingDetailEntitiesInRepo() => this._unitOfWork.Actors.GetAll();

        public override async Task<IEnumerable<MovieActorResult>> GetMovieDetailsFromApiAsync(int externalId)
            => await this._movieAPIClient.GetMovieActorsAsync(externalId);

        public override IEnumerable<Movie> GetMoviesWithoutDetails() => this._unitOfWork.Movies.GetMoviesWithoutActors();

        // explicit cast is defined in MovieCastMemberResult
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