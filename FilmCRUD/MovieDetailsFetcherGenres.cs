using System.Collections.Generic;
using System.Threading.Tasks;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class MovieDetailsFetcherGenres : MovieDetailsFetcherAbstract<Genre, MovieGenreResult>
    {
        public MovieDetailsFetcherGenres(IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient) : base(unitOfWork, movieAPIClient)
        {
        }

        public override IEnumerable<Genre> GetExistingEntitiesInRepo()
        {
            throw new System.NotImplementedException();
        }

        public override Task<IEnumerable<MovieGenreResult>> GetMovieDetailsFromApiAsync(int externalId)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<Movie> GetMoviesWithoutDetails()
        {
            throw new System.NotImplementedException();
        }
    }
}