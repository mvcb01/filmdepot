using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IMovieRipRepository : IEntityRepository<MovieRip>
    {
        IEnumerable<MovieRip> GetAllRipsInVisit(MovieWarehouseVisit visit);

        IEnumerable<MovieRip> GetAllRipsForMovie(Movie movie);

    }
}