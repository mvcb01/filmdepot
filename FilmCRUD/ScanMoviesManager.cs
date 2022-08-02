using System.Collections.Generic;
using System.Linq;
using System;

using FilmDomain.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Extensions;

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

        /// <summary>
        /// Method <c>GetMoviesWithReleaseDates</c> returns all the movies that have its ReleaseDate in
        /// <paramref name="dates"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithReleaseDates(MovieWarehouseVisit visit, params int[] dates)
        {
            IEnumerable<Movie> moviesInVisit = this._unitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => dates.Contains(m.ReleaseDate));
        }

        public IEnumerable<KeyValuePair<Genre, int>> GetCountByGenre(MovieWarehouseVisit visit)
        {
            IEnumerable<Movie> moviesInVisit = this._unitOfWork.Movies.GetAllMoviesInVisit(visit);

            // flatten -> group by Genre and count
            IEnumerable<IGrouping<Genre, Genre>> grouped = moviesInVisit.SelectMany(m => m.Genres).GroupBy(g => g);
            return grouped.Select(group => new KeyValuePair<Genre, int>(group.Key, group.Count()));
        }

        public IEnumerable<KeyValuePair<Actor, int>> GetCountByActor(MovieWarehouseVisit visit)
        {
            IEnumerable<Movie> moviesInVisit = this._unitOfWork.Movies.GetAllMoviesInVisit(visit);

            // flatten -> group by Actor and count
            IEnumerable<IGrouping<Actor, Actor>> grouped = moviesInVisit.SelectMany(m => m.Actors).GroupBy(a => a);
            return grouped.Select(group => new KeyValuePair<Actor, int>(group.Key, group.Count()));
        }

        public IEnumerable<KeyValuePair<Director, int>> GetCountByDirector(MovieWarehouseVisit visit)
        {
            IEnumerable<Movie> moviesInVisit = this._unitOfWork.Movies.GetAllMoviesInVisit(visit);

            // flatten -> group by Director and count
            IEnumerable<IGrouping<Director, Director>> grouped = moviesInVisit.SelectMany(m => m.Directors).GroupBy(a => a);
            return grouped.Select(group => new KeyValuePair<Director, int>(group.Key, group.Count()));
        }

        public MovieWarehouseVisit GetClosestVisit()
        {
            return this._unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();
        }

        public MovieWarehouseVisit GetClosestVisit(DateTime dt)
        {
            return this._unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(dt);
        }

        public IEnumerable<DateTime> ListVisitDates()
        {
            return this._unitOfWork.MovieWarehouseVisits.GetAll().GetVisitDates();
        }

        public IEnumerable<Genre> GenresFromName(string name)
        {
            return this._unitOfWork.Genres.GetGenresFromName(name);
        }

        public IEnumerable<Actor> GetActorsFromName(string name)
        {
            return this._unitOfWork.Actors.GetActorsFromName(name);
        }

        public IEnumerable<Director> GetDirectorsFromName(string name)
        {
            return this._unitOfWork.Directors.GetDirectorsFromName(name);
        }
    }
}