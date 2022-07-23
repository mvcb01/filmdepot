using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmDataAccess.EFCore.Repositories
{
    public class DirectorRepository : GenericRepository<Director>, IDirectorRepository
    {
        public DirectorRepository(SQLiteAppContext context) : base(context)
        {
        }

        public IEnumerable<Director> GetDirectorsFromName(string name)
        {
            IEnumerable<string> nameTokens = name.GetStringTokensWithoutPunctuationAndDiacritics();
            string nameLike = "%" + string.Join('%', nameTokens) + "%";

            // obs: in EF Core 6 we can use Regex.IsMatch in the Where method:
            //      https://docs.microsoft.com/en-us/ef/core/providers/sqlite/functions
            return _context.Directors.Where(d => EF.Functions.Like(d.Name, nameLike));
        }
    }
}