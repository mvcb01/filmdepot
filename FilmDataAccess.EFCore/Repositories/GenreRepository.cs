using System.Collections.Generic;
using System.Linq;
using FilmDomain.Entities;
using FilmDomain.Extensions;
using FilmDomain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmDataAccess.EFCore.Repositories
{
    public class GenreRepository : GenericRepository<Genre>, IGenreRepository
    {
        public GenreRepository(SQLiteAppContext context) : base(context)
        {
        }

        public Genre FindByExternalId(int externalId)
        {
            return _context.Genres.Where(m => m.ExternalId == externalId).FirstOrDefault();
        }

        public IEnumerable<Genre> GetGenresFromName(string name)
        {
            IEnumerable<string> nameTokens = name.GetStringTokensWithoutPunctuationAndDiacritics();
            string nameLike = "%" + string.Join('%', nameTokens) + "%";

            // obs: in EF Core 6 we can use Regex.IsMatch in the Where method:
            //      https://docs.microsoft.com/en-us/ef/core/providers/sqlite/functions
            return _context.Genres.Where(g => EF.Functions.Like(g.Name, nameLike));
        }

    }
}