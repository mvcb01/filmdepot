using System.Collections.Generic;
using System.Linq;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using System;

namespace FilmCRUD
{
    public class ScanManager
    {
        private IUnitOfWork unitOfWork { get; init; }

        public ScanManager(IUnitOfWork unitOfWork)
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

        public IEnumerable<string> GetAllRipsWithReleaseDate(string releaseDate)
        {
            return unitOfWork.MovieRips.Find(r => r.ParsedReleaseDate == releaseDate.Trim()).GetFileNames();
        }

        public Dictionary<DateTime, int> GetRipCountByVisit()
        {
            IEnumerable<MovieWarehouseVisit> allVisits = unitOfWork.MovieWarehouseVisits.GetAll();

            return allVisits.ToDictionary(
                visit => visit.VisitDateTime,
                visit => visit.MovieRips.Count()
                );
        }

    }
}