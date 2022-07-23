using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmDataAccess.EFCore.Repositories
{
    public class ActorRepository : GenericRepository<Actor>, IActorRepository
    {
        public ActorRepository(SQLiteAppContext context) : base(context)
        {
        }

        public Actor FindByExternalId(int externalId)
        {
            return _context.Actors.Where(m => m.ExternalId == externalId).FirstOrDefault();
        }

        public IEnumerable<Actor> GetActorsFromName(string name)
        {
            IEnumerable<string> nameTokens = name.GetStringTokensWithoutPunctuationAndDiacritics();
            string nameLike = "%" + string.Join('%', nameTokens) + "%";

            // obs: in EF Core 6 we can use Regex.IsMatch in the Where method:
            //      https://docs.microsoft.com/en-us/ef/core/providers/sqlite/functions
            return _context.Actors.Where(a => EF.Functions.Like(a.Name, nameLike));
        }
    }
}