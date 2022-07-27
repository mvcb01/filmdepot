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
            IEnumerable<string> nameTokens = name.GetStringTokensWithoutPunctuation(removeDiacritics: false);
            string nameLike = "%" + string.Join('%', nameTokens) + "%";

            IEnumerable<Director> result =  _context.Directors.Where(a => EF.Functions.Like(a.Name, nameLike));

            // searches again without diacritics if no results are found
            if (!result.Any())
            {
                result = _context.Directors.GetEntitiesFromName(name, removeDiacritics: true);
            }
            return result;
        }
    }
}