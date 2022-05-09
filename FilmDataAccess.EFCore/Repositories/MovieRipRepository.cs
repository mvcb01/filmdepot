using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using System.Linq;

namespace FilmDataAccess.EFCore.Repositories
{
    public class MovieRipRepository : GenericRepository<MovieRip>, IMovieRipRepository
    {
        public MovieRipRepository(SQLiteAppContext context) : base(context)
        {
        }

        public IEnumerable<MovieRip> GetAllRipsForMovie(Movie movie)
        {
            return _context.MovieRips.Where(mr => mr.Movie == movie);
        }

        public IEnumerable<MovieRip> GetAllRipsInVisit(MovieWarehouseVisit visit)
        {
            return visit.MovieRips;
        }

    }
}