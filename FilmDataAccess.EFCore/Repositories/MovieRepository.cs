using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmDataAccess.EFCore.Repositories
{
    public class MovieRepository : GenericRepository<Movie>, IMovieRepository
    {
        public MovieRepository(SQLiteAppContext context) : base(context)
        {
        }

        public IEnumerable<Movie> GetMoviesByGenre(params Genre[] genres)
        {
            return _context.Movies.Where(m => m.Genres.Intersect(genres).Count() > 0);
        }
    }
}