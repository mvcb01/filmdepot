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

        public IEnumerable<Movie> GetMoviesWithoutCastMembers() => this._context.Movies.Where(m => !m.CastMembers.Any());

        public IEnumerable<Movie> GetMoviesWithoutDirectors() => this._context.Movies.Where(m => !m.Directors.Any());

        public IEnumerable<Movie> GetMoviesWithoutGenres() => this._context.Movies.Where(m => !m.Genres.Any());

        public IEnumerable<Movie> GetMoviesWithoutImdbId() => this._context.Movies.Where(m => m.IMDBId == null);

        public IEnumerable<Movie> GetMoviesWithoutKeywords() => this._context.Movies.Where(m => m.Keywords == null);

        public IEnumerable<Movie> SearchMoviesWithTitle(string title) => this._context.Movies.GetMovieEntitiesFromTitleFuzzyMatching(title, removeDiacritics: true);

        public IEnumerable<Movie> GetAllMoviesInVisit(MovieWarehouseVisit visit)
        {
            // calling Distinct since, in the same visit, we can have more than one MovieRip entity refering to the same Movie entity:
            //      Haxan.1922.1080p.BluRay.x264-CiNEFiLE
            //      Haxan.1922.1080p.HDRip.x264.AAC-RARBG
            return visit.MovieRips.Select(r => r.Movie).Where(m => m != null).Distinct();
        }

    }
}