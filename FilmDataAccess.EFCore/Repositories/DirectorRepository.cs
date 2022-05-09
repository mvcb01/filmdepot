using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace FilmDataAccess.EFCore.Repositories
{
    public class DirectorRepository : GenericRepository<Director>, IDirectorRepository
    {
        public DirectorRepository(SQLiteAppContext context) : base(context)
        {
        }

        public IEnumerable<Director> GetMostRippedDirectors(int topN)
        {
            return _context.Directors.OrderByDescending(d => d.Movies.Count()).Take(topN);
        }
    }
}