using System.Collections.Generic;
using System.Threading.Tasks;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class MovieDetailsFetcherDirectors : MovieDetailsFetcherAbstract<Director, MovieDirectorResult>
    {
        public MovieDetailsFetcherDirectors(IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient) : base(unitOfWork, movieAPIClient)
        {
        }

        public override IEnumerable<Director> GetExistingDetailEntitiesInRepo()
        {
            return this._unitOfWork.Directors.GetAll();
        }

        public override async Task<IEnumerable<MovieDirectorResult>> GetMovieDetailsFromApiAsync(int externalId)
        {
            return await this._movieAPIClient.GetMovieDirectorsAsync(externalId);
        }

        public override IEnumerable<Movie> GetMoviesWithoutDetails()
        {
            return this._unitOfWork.Movies.GetMoviesWithoutDirectors();
        }

        public override Director CastApiResultToDetailEntity(MovieDirectorResult apiresult)
        {
            // explicit cast is defined in MovieDirectorResult
            return (Director)apiresult;
        }

        public override void AddDetailsToMovieEntity(Movie movie, IEnumerable<Director> details)
        {
            // ICollection does not necessarily have the AddRange method
            foreach (var director in details)
            {
                movie.Directors.Add(director);
            }
        }

    }
}