using System.Collections.Generic;
using System.Linq;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using System;

namespace FilmCRUD
{
    public class ScanRipsManager : GeneralScanManager
    {
        public ScanRipsManager(IUnitOfWork unitOfWork) : base(unitOfWork)
        { }

        public Dictionary<string, int> GetRipCountByReleaseDate(MovieWarehouseVisit visit)
        {
            Dictionary<string, int> result = new();

            IEnumerable<IGrouping<string, MovieRip>> gbReleaseDate = visit.MovieRips
                .GroupBy(rip => rip.ParsedReleaseDate ?? "empty");

            foreach (var dateGroup in gbReleaseDate)
            {
                result.Add(dateGroup.Key, dateGroup.Count());
            }
            return result;
        }

        public IEnumerable<string> GetAllRipsWithReleaseDate(MovieWarehouseVisit visit, params int[] dates)
        {
            string[] dateStrings = dates.Select(d => d.ToString()).ToArray();
            return visit.MovieRips
                .Where(r => dateStrings.Contains(r.ParsedReleaseDate))
                .Select(r => r.FileName);
        }

        public Dictionary<DateTime, int> GetRipCountByVisit()
        {
            IEnumerable<MovieWarehouseVisit> allVisits = UnitOfWork.MovieWarehouseVisits.GetAll();

            return allVisits.ToDictionary(
                visit => visit.VisitDateTime,
                visit => visit.MovieRips.Count()
                );
        }

        public Dictionary<string, IEnumerable<string>> GetLastVisitDiff()
        {
            MovieWarehouseVisit lastVisit = this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();
            MovieWarehouseVisit previousVisit = this.UnitOfWork.MovieWarehouseVisits.GetPreviousMovieWarehouseVisit(lastVisit);
            return GetVisitDiff(previousVisit, lastVisit);
        }

        public Dictionary<string, IEnumerable<string>> GetVisitDiff(MovieWarehouseVisit visitLeft, MovieWarehouseVisit visitRight)
        {
            if (visitRight == null)
            {
                throw new ArgumentNullException(nameof(visitRight));
            }

            if (visitLeft == null)
            {
                return new Dictionary<string, IEnumerable<string>>() {
                    ["added"] = visitRight.MovieRips.GetFileNames(),
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

            List<string> addedFileNames = visitRight.MovieRips.GetFileNames().Except(visitLeft.MovieRips.GetFileNames()).ToList();
            List<string> removedFileNames = visitLeft.MovieRips.GetFileNames().Except(visitRight.MovieRips.GetFileNames()).ToList();
            return new Dictionary<string, IEnumerable<string>>() {
                ["added"] = addedFileNames,
                ["removed"] = removedFileNames
            };
        }

    }
}
