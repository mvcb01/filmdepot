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
        protected IUnitOfWork _unitOfWork { get; init; }

        public GeneralScanManager(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
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

    }
}