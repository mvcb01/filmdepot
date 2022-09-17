using System.Collections.Generic;
using System.Threading.Tasks;
using FilmCRUD.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class MovieDetailsFetcherDirectors : MovieDetailsFetcherAbstract<Director, MovieDirectorResult>
    {
        public MovieDetailsFetcherDirectors(IUnitOfWork unitOfWork, IFileSystemIOWrapper fileSystemIOWrapper, IMovieAPIClient movieAPIClient)
            : base(unitOfWork, fileSystemIOWrapper, movieAPIClient)
        { }

        public override IEnumerable<Director> GetExistingDetailEntitiesInRepo() => this._unitOfWork.Directors.GetAll();

        public override async Task<IEnumerable<MovieDirectorResult>> GetMovieDetailsFromApiAsync(int externalId)
        {
            return await this._movieAPIClient.GetMovieDirectorsAsync(externalId);
        }

        public override IEnumerable<Movie> GetMoviesWithoutDetails() => this._unitOfWork.Movies.GetMoviesWithoutDirectors();

        // explicit cast is defined in MovieDirectorResult
        public override Director CastApiResultToDetailEntity(MovieDirectorResult apiresult) => (Director)apiresult;

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