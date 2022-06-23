using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
            return this._unitOfWork.Genres.GetAll();
        }

        public override async Task<IEnumerable<MovieGenreResult>> GetMovieDetailsFromApiAsync(int externalId)
        {
            return await this._movieAPIClient.GetMovieGenresAsync(externalId);
        }

        public override IEnumerable<Movie> GetMoviesWithoutDetails()
        {
            return this._unitOfWork.Movies.Find(m => m.Genres == null || !m.Genres.Any());
        }
    }
}