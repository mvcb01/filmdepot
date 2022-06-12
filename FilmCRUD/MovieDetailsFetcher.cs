using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class MovieDetailsFetcher
    {
        private IUnitOfWork _unitOfWork { get; init; }

        private IMovieAPIClient _movieAPIClient { get; init; }

        public MovieDetailsFetcher(IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
            this._movieAPIClient = movieAPIClient;
        }

    }
}