using System;

namespace FilmDomain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IMovieRepository Movies { get; }

        IGenreRepository Genres { get; }

        IMovieRipRepository MovieRips { get; }

        IMovieWarehouseVisitRepository MovieWarehouseVisits { get; }

        int Complete();
    }
}