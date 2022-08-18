using System;
using System.Collections.Generic;
using FilmDomain.Entities;

namespace FilmDomain.Interfaces
{
    public interface IMovieWarehouseVisitRepository : IEntityRepository<MovieWarehouseVisit>
    {
        MovieWarehouseVisit GetClosestMovieWarehouseVisit();

        MovieWarehouseVisit GetClosestMovieWarehouseVisit(DateTime dt);

        MovieWarehouseVisit GetPreviousMovieWarehouseVisit(MovieWarehouseVisit visit);
    }
}