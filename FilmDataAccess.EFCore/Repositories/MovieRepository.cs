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

        public IEnumerable<Movie> GetMoviesByGenre(params Genre[] genres)
        {
            return _context.Movies.Where(m => m.Genres.Intersect(genres).Count() > 0);
        }

        public IEnumerable<Movie> GetMoviesWithoutGenres()
        {
            return _context.Movies.Where(m => !m.Genres.Any());
        }

        public IEnumerable<Movie> SearchMoviesWithTitle(string title)
        {
            IEnumerable<string> titleTokens = title.GetStringTokensWithoutPunctuation();
            string titleLike = "%" + string.Join('%', titleTokens) + "%";

            // obs: no EF Core 6 já podemos usar Regex.IsMatch no Where
            return _context.Movies.Where(m => EF.Functions.Like(m.Title, titleLike));
        }
    }
}