using FilmDomain.Interfaces;
using FilmDomain.Entities;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FilmDataAccess.EFCore.Repositories
{
    public class MovieWarehouseVisitRepository : GenericRepository<MovieWarehouseVisit>, IMovieWarehouseVisitRepository
    {
        public MovieWarehouseVisitRepository(SQLiteAppContext context) : base(context) { }

        public MovieWarehouseVisit GetClosestMovieWarehouseVisit() => GetClosestMovieWarehouseVisit(DateTime.UtcNow);

        public IEnumerable<DateTime> GetVisitDates() => this._context.MovieWarehouseVisits.Select(v => v.VisitDateTime);

        public MovieWarehouseVisit GetClosestMovieWarehouseVisit(DateTime dt)
        {
            IEnumerable<DateTime> allDatetimes = this._context.MovieWarehouseVisits.Select(visit => visit.VisitDateTime).Distinct();
            return Find(visit => visit.VisitDateTime == GetClosestDatetime(allDatetimes, dt)).First();
        }

        public MovieWarehouseVisit GetPreviousMovieWarehouseVisit(MovieWarehouseVisit visit)
        {
            return this._context.MovieWarehouseVisits
                .Where(v => v.VisitDateTime < visit.VisitDateTime)
                .OrderByDescending(v => v.VisitDateTime)
                .FirstOrDefault();
        }

        private static DateTime GetClosestDatetime(IEnumerable<DateTime> allDateTimes, DateTime dt)
        {
            if (allDateTimes.Count() == 0)
            {
                throw new Exception("Argument allDateTimes is empty");
            }

            return allDateTimes.OrderBy(_dt => Math.Abs((_dt - dt).Ticks)).First();
        }
    }
}