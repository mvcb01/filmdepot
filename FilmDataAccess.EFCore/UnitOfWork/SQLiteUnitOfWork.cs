using FilmDataAccess.EFCore.Repositories;
using FilmDomain.Interfaces;

namespace FilmDataAccess.EFCore.UnitOfWork
{
    public class SQLiteUnitOfWork : IUnitOfWork
    {
        private readonly SQLiteAppContext _context;

        public IMovieRepository Movies { get; init; }

        public IDirectorRepository Directors { get; init; }

        public IGenreRepository Genres { get; init; }

        public IMovieRipRepository MovieRips { get; init; }

        public IMovieWarehouseVisitRepository MovieWarehouseVisits { get; init; }

        public IActorRepository Actors { get; init; }

        public SQLiteUnitOfWork(SQLiteAppContext context)
        {
            _context = context;
            Movies = new MovieRepository(_context);
            Directors = new DirectorRepository(_context);
            Genres = new GenreRepository(_context);
            MovieRips = new MovieRipRepository(_context);
            MovieWarehouseVisits = new MovieWarehouseVisitRepository(_context);
            Actors = new ActorRepository(_context);
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}