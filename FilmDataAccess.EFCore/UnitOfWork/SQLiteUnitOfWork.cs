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
            this._context = context;
            Movies = new MovieRepository(this._context);
            Directors = new DirectorRepository(this._context);
            Genres = new GenreRepository(this._context);
            MovieRips = new MovieRipRepository(this._context);
            MovieWarehouseVisits = new MovieWarehouseVisitRepository(this._context);
            Actors = new ActorRepository(this._context);
        }

        public int Complete() => this._context.SaveChanges();

        public void Dispose() => this._context.Dispose();
    }
}