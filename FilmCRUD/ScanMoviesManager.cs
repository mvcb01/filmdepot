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
        /// <summary>
        /// Utility methods to query info about the <see cref="Movie"/> entities in the repository. Methods where the first
        /// parameter is a <see cref="MovieWarehouseVisit"/> object return info about the provided visit.
        /// </summary>
        /// <param name="unitOfWork"></param>
        public ScanMoviesManager(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        /// <summary>
        /// Returns all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/> that have at least
        /// one genre in param <paramref name="genres"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithGenres(MovieWarehouseVisit visit, params Genre[] genres)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => genres.Intersect(m.Genres).Any());
        }

        /// <summary>
        /// Returns all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/> that have at least
        /// one cast member in param <paramref name="castMembers"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithCastMembers(MovieWarehouseVisit visit, params CastMember[] castMembers)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => castMembers.Intersect(m.CastMembers).Any());
        }

        /// <summary>
        /// Returns all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/> that have at least one
        /// director in param <paramref name="directors"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithDirectors(MovieWarehouseVisit visit, params Director[] directors)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => directors.Intersect(m.Directors).Any());
        }

        /// <summary>
        /// Returns all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/>
        /// with release date in param <paramref name="dates"/>.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithReleaseDates(MovieWarehouseVisit visit, params int[] dates)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            return moviesInVisit.Where(m => dates.Contains(m.ReleaseDate));
        }

        /// <summary>
        /// Returns all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/>
        /// with at least one keyword in param <paramref name="keywords"/>. Keywords are trimmed and comparison is case insensitive.
        /// </summary>
        public IEnumerable<Movie> GetMoviesWithKeywords(MovieWarehouseVisit visit, params string[] keywords)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            keywords = keywords.Select(k => k.Trim().ToLower()).ToArray();
            return moviesInVisit.Where(m => m.Keywords?.Intersect(keywords).Any() ?? false);
        }

        /// <summary>
        /// Group by and count by genre considering all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<Genre, int>> GetCountByGenre(MovieWarehouseVisit visit, out int withoutGenres)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);

            withoutGenres = moviesInVisit.Where(m => !m.Genres.Any()).Count();

            // flatten -> group by Genre and count
            IEnumerable<IGrouping<Genre, Genre>> grouped = moviesInVisit.SelectMany(m => m.Genres).GroupBy(g => g);
            return grouped.Select(group => new KeyValuePair<Genre, int>(group.Key, group.Count()));
        }

        /// <summary>
        /// Group by and count by cast member considering all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<CastMember, int>> GetCountByCastMember(MovieWarehouseVisit visit, out int withoutCastMembers)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);

            withoutCastMembers = moviesInVisit.Where(m => !m.CastMembers.Any()).Count();

            // flatten -> group by CastMember and count
            IEnumerable<IGrouping<CastMember, CastMember>> grouped = moviesInVisit.SelectMany(m => m.CastMembers).GroupBy(a => a);
            return grouped.Select(group => new KeyValuePair<CastMember, int>(group.Key, group.Count()));
        }

        /// <summary>
        /// Group by and count by director considering all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<Director, int>> GetCountByDirector(MovieWarehouseVisit visit, out int withoutDirectors)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);

            withoutDirectors = moviesInVisit.Where(m => !m.Directors.Any()).Count();

            // flatten -> group by Director and count
            IEnumerable<IGrouping<Director, Director>> grouped = moviesInVisit.SelectMany(m => m.Directors).GroupBy(a => a);
            return grouped.Select(group => new KeyValuePair<Director, int>(group.Key, group.Count()));
        }

        /// <summary>
        /// Group by and count by release date considering all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<int, int>> GetCountbyReleaseDate(MovieWarehouseVisit visit, out int withoutReleaseDate)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);

            withoutReleaseDate = moviesInVisit.Where(m => m.ReleaseDate == default).Count();

            IEnumerable<IGrouping<int, Movie>> grouped = moviesInVisit
                .Where(m => m.ReleaseDate != default)
                .GroupBy(m => m.ReleaseDate);
                
            return grouped.Select(group => new KeyValuePair<int, int>(group.Key, group.Count()));
        }

        /// <summary>
        /// Fuzzy match search on all the <see cref="Movie"/> entities linked to some movie rip in <paramref name="visit"/>. Both
        /// properties <see cref="Movie.Title"/> and <see cref="Movie.OriginalTitle"/> are used in the search.
        /// </summary>
        public IEnumerable<Movie> SearchMovieEntities(MovieWarehouseVisit visit, string title)
        {
            IEnumerable<Movie> moviesInVisit = this.UnitOfWork.Movies.GetAllMoviesInVisit(visit);
            IEnumerable<Movie> fuzzyMatchTitle = moviesInVisit.GetMovieEntitiesFromTitleFuzzyMatching(title, removeDiacritics: true);
            IEnumerable<Movie> fuzzyMatchOriginalTitle = moviesInVisit.GetMovieEntitiesFromOriginalTitleFuzzyMatching(title, removeDiacritics: true);
            return fuzzyMatchTitle.Concat(fuzzyMatchOriginalTitle).Distinct(); 
        }

        /// <summary>
        /// Returns all <see cref="Genre"/> entities in the repository that fuzzy match parameter <paramref name="name"/>.
        /// </summary>
        public IEnumerable<Genre> GenresFromName(string name) => this.UnitOfWork.Genres.GetGenresFromName(name);

        /// <summary>
        /// Returns all <see cref="CastMember"/> entities in the repository that fuzzy match parameter <paramref name="name"/>.
        /// </summary>
        public IEnumerable<CastMember> GetCastMembersFromName(string name) => this.UnitOfWork.CastMembers.GetCastMembersFromName(name);

        /// <summary>
        /// Returns all <see cref="Director"/> entities in the repository that fuzzy match parameter <paramref name="name"/>.
        /// </summary>
        public IEnumerable<Director> GetDirectorsFromName(string name) => this.UnitOfWork.Directors.GetDirectorsFromName(name);

        /// <summary>
        /// Considers all the distinct <see cref="Movie"/> entities linked to some <see cref="MovieRip"/> in the last two visits and outputs
        /// a dictionary with the keys "added" and "removed" where the values are the set difference of <see cref="MovieRip.FileName"/>'s
        /// between both visits.
        /// </summary>
        public Dictionary<string, IEnumerable<string>> GetLastVisitDiff()
        {
            MovieWarehouseVisit lastVisit = this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();
            MovieWarehouseVisit previousVisit = this.UnitOfWork.MovieWarehouseVisits.GetPreviousMovieWarehouseVisit(lastVisit);
            return GetVisitDiff(previousVisit, lastVisit);
        }

        /// <summary>
        /// Considers all the distinct <see cref="Movie"/> entities linked to some
        /// <see cref="MovieRip"/> in <paramref name="visitLeft"/> or in <paramref name="visitLeft"/> and outputs
        /// a dictionary with the keys "added" and "removed" where the values are the set difference of <see cref="MovieRip.FileName"/>'s
        /// between both visits.
        /// </summary>
        public Dictionary<string, IEnumerable<string>> GetVisitDiff(MovieWarehouseVisit visitLeft, MovieWarehouseVisit visitRight)
        {
            if (visitRight is null) throw new ArgumentNullException(nameof(visitRight));

            if (visitLeft is null) return new Dictionary<string, IEnumerable<string>>() {
                ["added"] = this.UnitOfWork.Movies.GetAllMoviesInVisit(visitRight).Select(m => m.ToString()),
                ["removed"] = Enumerable.Empty<string>()
            };

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