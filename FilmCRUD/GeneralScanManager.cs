using System;
using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace FilmCRUD
{
    /// <summary>
    /// Provides useful methods to derived scan classes.
    /// </summary>
    public class GeneralScanManager
    {
        protected readonly IUnitOfWork UnitOfWork;

        public GeneralScanManager(IUnitOfWork unitOfWork) => this.UnitOfWork = unitOfWork;

        /// <summary>
        /// Finds and returns the <see cref="MovieWarehouseVisit"/> in the repository with the closest visit datetime
        /// to the current datetime.
        /// </summary>
        public MovieWarehouseVisit GetClosestVisit() => this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit();

        /// <summary>
        /// Finds and returns the <see cref="MovieWarehouseVisit"/> in the repository with the closest visit datetime
        /// to the provided parameter <paramref name="dt"/>.
        /// </summary>
        public MovieWarehouseVisit GetClosestVisit(DateTime dt) => this.UnitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(dt);

        /// <summary>
        /// Finds and returns the most recent <see cref = "MovieWarehouseVisit"/> in the repository among those
        /// with an older visit datetime than the provided <paramref name="visit"/>. Returns <see langword="null"/> if no such
        /// visit is found.
        /// </summary>
        public MovieWarehouseVisit GetPreviousVisit(MovieWarehouseVisit visit) => this.UnitOfWork.MovieWarehouseVisits.GetPreviousMovieWarehouseVisit(visit);
    }
}