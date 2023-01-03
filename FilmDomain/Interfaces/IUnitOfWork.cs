using System;

namespace FilmDomain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IMovieRepository Movies { get; }

        IDirectorRepository Directors { get; }

        IGenreRepository Genres { get; }

        IMovieRipRepository MovieRips { get; }

        IMovieWarehouseVisitRepository MovieWarehouseVisits { get; }

        ICastMemberRepository CastMembers { get; }

        int Complete();
    }
}