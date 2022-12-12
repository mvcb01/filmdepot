using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;


namespace FilmDataAccess.EFCore.Repositories
{
    public class MovieRepository : GenericRepository<Movie>, IMovieRepository
    {
        public MovieRepository(SQLiteAppContext context) : base(context) { }

        public Movie FindByExternalId(int externalId) => this._context.Movies.Where(m => m.ExternalId == externalId).FirstOrDefault();

        public IEnumerable<Movie> GetMoviesWithoutActors() => this._context.Movies.Where(m => !m.Actors.Any());

        public IEnumerable<Movie> GetMoviesWithoutDirectors() => this._context.Movies.Where(m => !m.Directors.Any());

        public IEnumerable<Movie> GetMoviesWithoutGenres() => this._context.Movies.Where(m => !m.Genres.Any());

        public IEnumerable<Movie> GetMoviesWithoutImdbId() => this._context.Movies.Where(m => m.IMDBId == null);

        public IEnumerable<Movie> GetMoviesWithoutKeywords() => this._context.Movies.Where(m => m.Keywords == null);

        public IEnumerable<Movie> SearchMoviesWithTitle(string title) => this._context.Movies.GetMovieEntitiesFromTitleFuzzyMatching(title, removeDiacritics: true);

        public IEnumerable<Movie> GetAllMoviesInVisit(MovieWarehouseVisit visit) => visit.MovieRips.Select(r => r.Movie).Where(m => m != null);

    }
}