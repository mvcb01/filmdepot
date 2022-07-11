using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmDataAccess.EFCore.Repositories
{
    public class MovieRepository : GenericRepository<Movie>, IMovieRepository
    {
        public MovieRepository(SQLiteAppContext context) : base(context)
        {
        }

        public Movie FindByExternalId(int externalId)
        {
            return _context.Movies.Where(m => m.ExternalId == externalId).FirstOrDefault();
        }

        public IEnumerable<Movie> GetMoviesWithoutActors()
        {
            return _context.Movies.Where(m => !m.Actors.Any());
        }

        public IEnumerable<Movie> GetMoviesWithoutDirectors()
        {
            return _context.Movies.Where(m => !m.Directors.Any());
        }

        public IEnumerable<Movie> GetMoviesWithoutGenres()
        {
            return _context.Movies.Where(m => !m.Genres.Any());
        }

        public IEnumerable<Movie> GetMoviesWithoutImdbId()
        {
            return _context.Movies.Where(m => m.IMDBId == null);
        }

        public IEnumerable<Movie> GetMoviesWithoutKeywords()
        {
            return _context.Movies.Where(m => m.Keywords == null);
        }

        public IEnumerable<Movie> SearchMoviesWithTitle(string title)
        {
            IEnumerable<string> titleTokens = title.GetStringTokensWithoutPunctuation();
            string titleLike = "%" + string.Join('%', titleTokens) + "%";

            // obs: no EF Core 6 já podemos usar Regex.IsMatch no Where
            return _context.Movies.Where(m => EF.Functions.Like(m.Title, titleLike));
        }

        public IEnumerable<Movie> GetAllMoviesInVisit(MovieWarehouseVisit visit)
        {
            return visit.MovieRips.Select(r => r.Movie);
        }
    }
}