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
        protected IUnitOfWork UnitOfWork { get; init; }

        public GeneralScanManager(IUnitOfWork unitOfWork)
        {
            this.UnitOfWork = unitOfWork;
        }

        public MovieWarehouseVisit GetClosestVisit()
        {
            return this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();
        }

        public MovieWarehouseVisit GetClosestVisit(DateTime dt)
        {
            return this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(dt);
        }

        public MovieWarehouseVisit GetPreviousVisit(MovieWarehouseVisit visit)
        {
            return this.UnitOfWork.MovieWarehouseVisits.GetPreviousMovieWarehouseVisit(visit);
        }

        public IEnumerable<DateTime> ListVisitDates()
        {
            return this.UnitOfWork.MovieWarehouseVisits.GetAll().GetVisitDates();
        }

    }
}