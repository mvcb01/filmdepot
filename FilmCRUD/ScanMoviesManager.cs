using System.Collections.Generic;
using System.Linq;
using System;

using FilmDomain.Interfaces;
using FilmDomain.Entities;
using FilmDomain.Extensions;

namespace FilmCRUD
{
    public class ScanMoviesManager : GeneralScanManager
    {
        public ScanMoviesManager(IUnitOfWork unitOfWork) : base(unitOfWork)
        { }

        /// <summary>
        /// Method <c>GetMoviesWithGenres</c> returns all the movies that have at least
        /// one corresponding MovieRip in <paramref name="visit"/> and at least one Genre in <paramref name="genres"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithGenres(MovieWarehouseVisit visit, params Genre[] genres)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => genres.Intersect(m.Genres).Any());
        }

        /// <summary>
        /// Method <c>GetMoviesWithActors</c> returns all the movies that have at least
        /// one corresponding MovieRip in <paramref name="visit"/> and at least one Actor in <paramref name="actors"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithActors(MovieWarehouseVisit visit, params Actor[] actors)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => actors.Intersect(m.Actors).Any());
        }

        /// <summary>
        /// Method <c>GetMoviesWithDirectors</c> returns all the movies that have at least
        /// one corresponding MovieRip in <paramref name="visit"/> and at least one Director in <paramref name="directors"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithDirectors(MovieWarehouseVisit visit, params Director[] directors)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => directors.Intersect(m.Directors).Any());
        }

        /// <summary>
        /// Method <c>GetMoviesWithReleaseDates</c> returns all the movies that have its ReleaseDate in
        /// <paramref name="dates"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithReleaseDates(MovieWarehouseVisit visit, params int[] dates)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => dates.Contains(m.ReleaseDate));
        }

        public IEnumerable<KeyValuePair<Genre, int>> GetCountByGenre(MovieWarehouseVisit visit)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);

            // flatten -> group by Genre and count
            IEnumerable<IGrouping<Genre, Genre>> grouped = moviesInVisit.SelectMany(m => m.Genres).GroupBy(g => g);
            return grouped.Select(group => new KeyValuePair<Genre, int>(group.Key, group.Count()));
        }

        public IEnumerable<KeyValuePair<Actor, int>> GetCountByActor(MovieWarehouseVisit visit)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);

            // flatten -> group by Actor and count
            IEnumerable<IGrouping<Actor, Actor>> grouped = moviesInVisit.SelectMany(m => m.Actors).GroupBy(a => a);
            return grouped.Select(group => new KeyValuePair<Actor, int>(group.Key, group.Count()));
        }

        public IEnumerable<KeyValuePair<Director, int>> GetCountByDirector(MovieWarehouseVisit visit)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);

            // flatten -> group by Director and count
            IEnumerable<IGrouping<Director, Director>> grouped = moviesInVisit.SelectMany(m => m.Directors).GroupBy(a => a);
            return grouped.Select(group => new KeyValuePair<Director, int>(group.Key, group.Count()));
        }

        public IEnumerable<Movie> SearchMovieEntitiesByTitle(MovieWarehouseVisit visit, string title)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.GetMovieEntitiesFromTitleFuzzyMatching(title, removeDiacritics: true);
        }

        public IEnumerable<Genre> GenresFromName(string name)
        {
            return this.UnitOfWork.Genres.GetGenresFromName(name);
        }

        public IEnumerable<Actor> GetActorsFromName(string name)
        {
            return this.UnitOfWork.Actors.GetActorsFromName(name);
        }

        public IEnumerable<Director> GetDirectorsFromName(string name)
        {
            return this.UnitOfWork.Directors.GetDirectorsFromName(name);
        }

        public Dictionary<string, IEnumerable<string>> GetLastVisitDiff()
        {
            MovieWarehouseVisit lastVisit = this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();
            MovieWarehouseVisit previousVisit = this.UnitOfWork.MovieWarehouseVisits.GetPreviousMovieWarehouseVisit(lastVisit);
            return GetVisitDiff(previousVisit, lastVisit);
        }

        /// <summary>
        /// Method <c>GetVisitDiff</c> considers all the distinct Movie entities linked to some
        /// MovieRip in <paramref name="visitLeft"/> or in <paramref name="visitLeft"/> and outputs the difference in
        /// a dictionary with keys "added" and "removed".
        /// </summary>
        public Dictionary<string, IEnumerable<string>> GetVisitDiff(MovieWarehouseVisit visitLeft, MovieWarehouseVisit visitRight)
        {
            if (visitRight == null)
            {
                throw new ArgumentNullException("visitRight should not be null");
            }

            if (visitLeft == null)
            {
                return new Dictionary<string, IEnumerable<string>>() {
                    ["added"] = this.UnitOfWork.Movies.GetAllMoviesInVisit(visitRight).Select(m => m.ToString()),
                    ["removed"] = Enumerable.Empty<string>()
                };
            }

            if (visitLeft.VisitDateTime >= visitRight.VisitDateTime)
            {
                string leftString = visitLeft.VisitDateTime.ToString("MMMM dd yyyy");
                string rightString = visitRight.VisitDateTime.ToString("MMMM dd yyyy");
                string msg = "Expected visitLeft.VisitDateTime < visitRight.VisitDateTime, ";
                msg += $"got visitLeft.VisitDateTime = {leftString} and visitRight.VisitDateTime = {rightString}";
                throw new ArgumentException(msg);
            }

            IEnumerable<Movie> visitLeftMovies = this.UnitOfWork.Movies.GetAllMoviesInVisit(visitLeft);
            IEnumerable<Movie> visitRightMovies = this.UnitOfWork.Movies.GetAllMoviesInVisit(visitRight);
            return new Dictionary<string, IEnumerable<string>>() {
                ["removed"] = visitLeftMovies.Except(visitRightMovies).Select(m => m.ToString()),
                ["added"] = visitRightMovies.Except(visitLeftMovies).Select(m => m.ToString())
            };
        }
    }
}