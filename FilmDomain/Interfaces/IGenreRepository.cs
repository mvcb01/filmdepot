using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IGenreRepository : IEntityRepository<Genre>
    {
        Genre GetGenreFromName(string name);

        Genre FindByExternalId(int externalId);
    }
}