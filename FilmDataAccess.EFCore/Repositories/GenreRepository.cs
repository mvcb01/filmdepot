using System.Collections.Generic;
using System.Linq;
using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace FilmDataAccess.EFCore.Repositories
{
    public class GenreRepository : GenericRepository<Genre>, IGenreRepository
    {
        public GenreRepository(SQLiteAppContext context) : base(context)
        {
        }

        public Genre GetGenreFromName(string name)
        {
            return Find(g => g.Name.Contains(name)).FirstOrDefault();
        }

    }
}