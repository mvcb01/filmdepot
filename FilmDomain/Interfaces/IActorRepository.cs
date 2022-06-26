using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IActorRepository : IEntityRepository<Actor>
    {
        IEnumerable<Actor> GetActorsFromName(string name);

        Actor FindByExternalId(int externalId);

    }
}