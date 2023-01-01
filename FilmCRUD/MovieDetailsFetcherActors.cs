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
    public class MovieDetailsFetcherActors : MovieDetailsFetcherAbstract<CastMember, MovieCastMemberResult>
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

        public override IEnumerable<CastMember> GetExistingDetailEntitiesInRepo() => this._unitOfWork.CastMembers.GetAll();

        public override async Task<IEnumerable<MovieCastMemberResult>> GetMovieDetailsFromApiAsync(int externalId)
            => await this._movieAPIClient.GetMovieCastMembersAsync(externalId);

        public override IEnumerable<Movie> GetMoviesWithoutDetails() => this._unitOfWork.Movies.GetMoviesWithoutCastMembers();

        // explicit cast is defined in MovieCastMemberResult
        public override CastMember CastApiResultToDetailEntity(MovieCastMemberResult apiresult) => (CastMember)apiresult;

        public override void AddDetailsToMovieEntity(Movie movie, IEnumerable<CastMember> details)
        {
            // ICollection does not necessarily have the AddRange method
            foreach (var actor in details)
            {
                movie.CastMembers.Add(actor);
            }
        }

    }
}