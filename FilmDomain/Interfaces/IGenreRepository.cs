using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IGenreRepository : IEntityRepository<Genre>
    {
        IEnumerable<Genre> GetGenresFromName(string name);

        Genre FindByExternalId(int externalId);
    }
}