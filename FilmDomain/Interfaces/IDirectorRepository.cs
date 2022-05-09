using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IDirectorRepository : IEntityRepository<Director>
    {
        IEnumerable<Director> GetMostRippedDirectors(int topN);
    }
}