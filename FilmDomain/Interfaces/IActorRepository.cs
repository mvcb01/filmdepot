using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IActorRepository : IEntityRepository<CastMember>
    {
        IEnumerable<CastMember> GetActorsFromName(string name);

        CastMember FindByExternalId(int externalId);

    }
}