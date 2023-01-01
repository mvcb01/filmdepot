using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FilmDataAccess.EFCore.Repositories
{
    public class ActorRepository : GenericRepository<CastMember>, ICastMemberRepository
    {
        public ActorRepository(SQLiteAppContext context) : base(context) { }

        public CastMember FindByExternalId(int externalId) => this._context.Actors.Where(m => m.ExternalId == externalId).FirstOrDefault();

        public IEnumerable<CastMember> GetCastMembersFromName(string name)
        {
            IEnumerable<string> nameTokens = name.GetStringTokensWithoutPunctuation(removeDiacritics: false);
            string nameLike = "%" + string.Join('%', nameTokens) + "%";

            IEnumerable<CastMember> result =  this._context.Actors.Where(a => EF.Functions.Like(a.Name, nameLike));

            // searches again without diacritics if no results are found
            if (!result.Any())
            {
                result = _context.Actors.GetEntitiesFromNameFuzzyMatching(name, removeDiacritics: true);
            }
            return result;
        }
    }
}