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

        public ICastMemberRepository CastMembers { get; init; }

        public SQLiteUnitOfWork(SQLiteAppContext context)
        {
            this._context = context;
            this.Movies = new MovieRepository(this._context);
            this.Directors = new DirectorRepository(this._context);
            this.Genres = new GenreRepository(this._context);
            this.MovieRips = new MovieRipRepository(this._context);
            this.MovieWarehouseVisits = new MovieWarehouseVisitRepository(this._context);
            this.CastMembers = new ActorRepository(this._context);
        }

        public int Complete() => this._context.SaveChanges();

        public void Dispose() => this._context.Dispose();
    }
}