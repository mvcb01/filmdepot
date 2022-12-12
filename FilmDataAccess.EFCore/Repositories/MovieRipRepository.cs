using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using System.Linq;

namespace FilmDataAccess.EFCore.Repositories
{
    public class MovieRipRepository : GenericRepository<MovieRip>, IMovieRipRepository
    {
        public MovieRipRepository(SQLiteAppContext context) : base(context) { }

        public MovieRip FindByFileName(string fileName) => this._context.MovieRips.Where(mr => mr.FileName == fileName).FirstOrDefault();

        public IEnumerable<MovieRip> GetAllRipsForMovie(Movie movie) => this._context.MovieRips.Where(mr => mr.Movie == movie);

        public IEnumerable<MovieRip> GetAllRipsInVisit(MovieWarehouseVisit visit) => visit.MovieRips;
    }
}