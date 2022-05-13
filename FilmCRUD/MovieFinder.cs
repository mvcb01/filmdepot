using System.Linq;
using System.Collections.Generic;

using FilmDomain.Entities;
using MovieAPIClients;
using ConfigUtils.Interfaces;
using MovieAPIClients.Interfaces;
using FilmDomain.Interfaces;
using System.Threading.Tasks;

namespace FilmCRUD
{
    public class MovieFinder
    {
        private IUnitOfWork _unitOfWork { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        private IMovieAPIClient _movieAPIClient { get; init; }

        public MovieFinder(IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient, IAppSettingsManager appSettingsManager)
        {
            this._unitOfWork = unitOfWork;
            this._movieAPIClient = movieAPIClient;
            this._appSettingsManager = appSettingsManager;
        }

        public async Task<Movie> FindMovieOnlineAsync(string parsedTitle, string parsedReleaseDate = null)
        {
            List<MovieSearchResult> result = (await _movieAPIClient.SearchMovieAsync(parsedTitle)).ToList();
            return new Movie() ;
        }
    }

}