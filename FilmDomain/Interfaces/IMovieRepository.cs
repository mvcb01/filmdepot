using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IMovieRepository : IEntityRepository<Movie>
    {
        IEnumerable<Movie> SearchMoviesWithTitle(string title);

        Movie FindByExternalId(int externalId);

        IEnumerable<Movie> GetMoviesWithoutGenres();

        IEnumerable<Movie> GetMoviesWithoutActors();

        IEnumerable<Movie> GetMoviesWithoutDirectors();

        IEnumerable<Movie> GetMoviesWithoutKeywords();

        IEnumerable<Movie> GetMoviesWithoutImdbId();

        IEnumerable<Movie> GetAllMoviesInVisit(MovieWarehouseVisit visit);
    }
}