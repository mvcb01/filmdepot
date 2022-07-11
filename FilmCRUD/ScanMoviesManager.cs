using System.Collections.Generic;
using System.Linq;

using FilmDomain.Interfaces;
using FilmDomain.Entities;

namespace FilmCRUD
{


    public class ScanMoviesManager
    {
        private IUnitOfWork _unitOfWork { get; init; }

        public ScanMoviesManager(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }

        public IEnumerable<Movie> GetMoviesWithGenres(MovieWarehouseVisit visit, params Genre[] genres)
        {
            IEnumerable<Movie> moviesInVisit = visit.MovieRips.Select(r => r.Movie);
            return null;
        }
    }
}