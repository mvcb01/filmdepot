using System.Collections.Generic;
using System.Linq;

using FilmDomain.Interfaces;
using FilmDomain.Entities;

namespace FilmCRUD
{
    public class ScanMoviesManager
    {
        private IUnitOfWork _unitOfWork { get; init; }

        public ScanMoviesManager(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Method <c>GetMoviesWithGenres</c> returns all the movies that have at least
        /// one corresponding MovieRip in <paramref name="visit"/> and at least one Genre in <paramref name="genres"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithGenres(MovieWarehouseVisit visit, params Genre[] genres)
        {
            IEnumerable<Movie> moviesInVisit = this._unitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => genres.Intersect(m.Genres).Any());
        }

        /// <summary>
        /// Method <c>GetMoviesWithActors</c> returns all the movies that have at least
        /// one corresponding MovieRip in <paramref name="visit"/> and at least one Actor in <paramref name="actors"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithActors(MovieWarehouseVisit visit, params Actor[] actors)
        {
            IEnumerable<Movie> moviesInVisit = this._unitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => actors.Intersect(m.Actors).Any());
        }

        /// <summary>
        /// Method <c>GetMoviesWithDirectors</c> returns all the movies that have at least
        /// one corresponding MovieRip in <paramref name="visit"/> and at least one Director in <paramref name="directors"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithDirectors(MovieWarehouseVisit visit, params Director[] directors)
        {
            IEnumerable<Movie> moviesInVisit = this._unitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => directors.Intersect(m.Directors).Any());
        }

        public IEnumerable<KeyValuePair<Genre, int>> GetCountByGenre(MovieWarehouseVisit visit)
        {
            return this._unitOfWork.Genres.GetAll().Select(g => new KeyValuePair<Genre, int>(g, g.Movies.Count()));
        }
    }
}