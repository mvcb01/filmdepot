using System.Collections.Generic;
using System.Linq;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using System;

namespace FilmCRUD
{
    public class ScanRipsManager
    {
        private IUnitOfWork unitOfWork { get; init; }

        public ScanRipsManager(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public Dictionary<string, int> GetRipCountByReleaseDate()
        {
            Dictionary<string, int> result = new();
            MovieWarehouseVisit latestVisit = unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();

            IEnumerable<IGrouping<string, MovieRip>> gbReleaseDate = latestVisit.MovieRips
                .GroupBy(rip => rip.ParsedReleaseDate ?? "empty");

            foreach (var dateGroup in gbReleaseDate)
            {
                result.Add(dateGroup.Key, dateGroup.Count());
            }
            return result;
        }

        public IEnumerable<string> GetAllRipsWithReleaseDate(params int[] dates)
        {
            string[] dateStrings = dates.Select(d => d.ToString()).ToArray();
            MovieWarehouseVisit latestVisit = unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();
            return latestVisit.MovieRips
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
            var addedFileNames = new List<string>();
            var removedFileNames = new List<string>();

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
            else if (nVisits == 1)
            {
                addedFileNames = unitOfWork.MovieWarehouseVisits.GetAll().First().MovieRips.GetFileNames().ToList();
            }
            else
            {
                DateTime lastVisitDate = lastTwoVisitDates[0];
                DateTime firstVisitDate = lastTwoVisitDates[1];

                MovieWarehouseVisit lastVisit = unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(lastVisitDate);
                MovieWarehouseVisit firstVisit = unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(firstVisitDate);

                addedFileNames = lastVisit.MovieRips.GetFileNames().Except(firstVisit.MovieRips.GetFileNames()).ToList();
                removedFileNames = firstVisit.MovieRips.GetFileNames().Except(lastVisit.MovieRips.GetFileNames()).ToList();
            }
            return new Dictionary<string, IEnumerable<string>>() {
                ["added"] = addedFileNames,
                ["removed"] = removedFileNames
            };
        }
    }
}
