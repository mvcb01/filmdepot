using FilmDataAccess.EFCore.Repositories;
using FilmDomain.Interfaces;

namespace FilmDataAccess.EFCore.UnitOfWork
{
    public class SQLiteUnitOfWork : IUnitOfWork
    {
        private readonly SQLiteAppContext _context;

        public IMovieRepository Movies { get; init; }

        public IGenreRepository Genres { get; init; }

        public IMovieRipRepository MovieRips { get; init; }

        public IMovieWarehouseVisitRepository MovieWarehouseVisits { get; init; }

        public SQLiteUnitOfWork(SQLiteAppContext context)
        {
            _context = context;
            Movies = new MovieRepository(_context);
            Genres = new GenreRepository(_context);
            MovieRips = new MovieRipRepository(_context);
            MovieWarehouseVisits = new MovieWarehouseVisitRepository(_context);
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