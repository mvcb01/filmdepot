using System.Collections.Generic;
using System.Threading.Tasks;
using FilmCRUD.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class MovieDetailsFetcherGenres : MovieDetailsFetcherAbstract<Genre, MovieGenreResult>
    {
        public MovieDetailsFetcherGenres(IUnitOfWork unitOfWork, IFileSystemIOWrapper fileSystemIOWrapper, IMovieAPIClient movieAPIClient)
            : base(unitOfWork, fileSystemIOWrapper, movieAPIClient)
        { }

        public override IEnumerable<Genre> GetExistingDetailEntitiesInRepo() => this._unitOfWork.Genres.GetAll();

        public override async Task<IEnumerable<MovieGenreResult>> GetMovieDetailsFromApiAsync(int externalId)
        {
            return await this._movieAPIClient.GetMovieGenresAsync(externalId);
        }

        public override IEnumerable<Movie> GetMoviesWithoutDetails() => this._unitOfWork.Movies.GetMoviesWithoutGenres();

        // explicit cast is defined in MovieGenreResult
        public override Genre CastApiResultToDetailEntity(MovieGenreResult apiResult) => (Genre)apiResult;

        public override void AddDetailsToMovieEntity(Movie movie, IEnumerable<Genre> details)
        {
            // ICollection does not necessarily have the AddRange method
            foreach (var genre in details)
            {
                movie.Genres.Add(genre);
            }
        }

    }
}