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


        // override para incluir os Genres, Directors e MovieRips
        // public override IEnumerable<Movie> GetAll()
        // {
        //     return _context.Movies.Include(m => m.Genres).Include(m => m.Directors).Include(m => m.MovieRips);
        // }

        public IEnumerable<Movie> GetMoviesByGenre(params Genre[] genres)
        {
            var moviesWithGenres = GetMoviesWithGenres();
            return moviesWithGenres.Where(m => m.Genres.Intersect(genres).Count() > 0);
        }

        public IEnumerable<Movie> GetMoviesWithGenres()
        {
            return _context.Movies.Include(m => m.Genres);
        }
    }
}