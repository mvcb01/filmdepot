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
        protected readonly IUnitOfWork UnitOfWork;

        public GeneralScanManager(IUnitOfWork unitOfWork) => this.UnitOfWork = unitOfWork;

        public MovieWarehouseVisit GetClosestVisit() => this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();

        public MovieWarehouseVisit GetClosestVisit(DateTime dt) => this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(dt);

        public MovieWarehouseVisit GetPreviousVisit(MovieWarehouseVisit visit) => this.UnitOfWork.MovieWarehouseVisits.GetPreviousMovieWarehouseVisit(visit);
    }
}