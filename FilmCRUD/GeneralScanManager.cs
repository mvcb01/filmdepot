using System;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Extensions;
using FilmDomain.Interfaces;

namespace FilmCRUD
{
    // to provide some useful methods to other Scan classes
    public class GeneralScanManager
    {
        protected IUnitOfWork unitOfWork { get; init; }

        public GeneralScanManager(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public MovieWarehouseVisit GetClosestVisit()
        {
            return this.unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();
        }

        public MovieWarehouseVisit GetClosestVisit(DateTime dt)
        {
            return this.unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(dt);
        }

        public MovieWarehouseVisit GetPreviousVisit(MovieWarehouseVisit visit)
        {
            return this.unitOfWork.MovieWarehouseVisits.GetPreviousMovieWarehouseVisit(visit);
        }

        public IEnumerable<DateTime> ListVisitDates()
        {
            return this.unitOfWork.MovieWarehouseVisits.GetAll().GetVisitDates();
        }

    }
}