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
            IEnumerable<MovieWarehouseVisit> allVisits = unitOfWork.MovieWarehouseVisits.GetAll();

            return allVisits.ToDictionary(
                visit => visit.VisitDateTime,
                visit => visit.MovieRips.Count()
                );
        }

        public Dictionary<string, IEnumerable<string>> GetLastVisitDiff()
        {
            List<DateTime> lastTwoVisitDates = unitOfWork.MovieWarehouseVisits
                .GetAll()
                .GetVisitDates()
                .OrderByDescending(dt => dt)
                .Take(2)
                .ToList();

            int nVisits = lastTwoVisitDates.Count();
            if (nVisits == 0)
            {
                throw new InvalidOperationException("No visits yet!");
            }

            if (nVisits == 1)
            {
                return new Dictionary<string, IEnumerable<string>>() {
                    ["added"] = unitOfWork.MovieWarehouseVisits.GetAll().First().MovieRips.GetFileNames().ToList(),
                    ["removed"] = Enumerable.Empty<string>()
                };
            }

            DateTime visitDateRight = lastTwoVisitDates[0];
            DateTime visitDateLeft = lastTwoVisitDates[1];

            MovieWarehouseVisit visitRight = unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(visitDateRight);
            MovieWarehouseVisit visitLeft = unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(visitDateLeft);
            return GetVisitDiff(visitLeft, visitRight);
        }

        public Dictionary<string, IEnumerable<string>> GetVisitDiff(MovieWarehouseVisit visitLeft, MovieWarehouseVisit visitRight)
        {
            if (visitLeft.VisitDateTime >= visitRight.VisitDateTime)
            {
                string leftString = visitLeft.VisitDateTime.ToString("yyyyMMdd");
                string rightString = visitRight.VisitDateTime.ToString("yyyyMMdd");
                string msg = "Expected visitLeft.VisitDateTime >= visitRight.VisitDateTime, ";
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
