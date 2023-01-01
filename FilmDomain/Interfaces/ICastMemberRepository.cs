using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface ICastMemberRepository : IEntityRepository<CastMember>
    {
        IEnumerable<CastMember> GetCastMembersFromName(string name);

        CastMember FindByExternalId(int externalId);
    }
}