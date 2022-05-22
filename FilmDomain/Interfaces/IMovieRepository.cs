using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IMovieRepository : IEntityRepository<Movie>
    {
        IEnumerable<Movie> GetMoviesByGenre(params Genre[] genres);

    }
}