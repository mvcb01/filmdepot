using System;
using System.Collections.Generic;
using System.Linq;

using ConfigUtils.Interfaces;
using FilmCRUD.Helpers;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;

namespace FilmCRUD
{
    public class RipToMovieLinker
    {
        private IUnitOfWork _unitOfWork { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        private MovieFinder _movieFinder { get; init; }

        public RipToMovieLinker(IUnitOfWork unitOfWork, IAppSettingsManager appSettingsManager, IMovieAPIClient movieAPIClient)
        {
            this._movieFinder = new MovieFinder(movieAPIClient);
            this._unitOfWork = unitOfWork;
            this._appSettingsManager = appSettingsManager;
        }

        public void LinkMovieRipsToMovies()
        {
        }
    }

}