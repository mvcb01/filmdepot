using System;
using System.Collections.Generic;
using System.Linq;

using ConfigUtils.Interfaces;
using FilmCRUD.Helpers;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Interfaces;
using MovieAPIClients.Interfaces;
using System.IO;

namespace FilmCRUD
{
    public class RipToMovieLinker
    {
        private IFileSystemIOWrapper _fileSystemIOWrapper { get; init; }

        private IUnitOfWork _unitOfWork { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        public MovieFinder MovieFinder { get; init; }

        public RipToMovieLinker(
            IUnitOfWork unitOfWork,
            IFileSystemIOWrapper fileSystemIOWrapper,
            IAppSettingsManager appSettingsManager,
            IMovieAPIClient movieAPIClient)
        {
            this._unitOfWork = unitOfWork;
            this._fileSystemIOWrapper = fileSystemIOWrapper;
            this._appSettingsManager = appSettingsManager;
            this.MovieFinder = new MovieFinder(movieAPIClient);
        }

        public IEnumerable<MovieRip> GetMovieRipsToLink()
        {
            IEnumerable<string> ripFilenamesToIgnore = _appSettingsManager.GetRipFilenamesToIgnoreOnLinking();
            return _unitOfWork.MovieRips
                .Find(r => r.Movie == null)
                .Where(r => r.ParsedTitle != null && !ripFilenamesToIgnore.Contains(r.FileName));
        }

        public Movie FindRelatedMovieEntityInRepo(MovieRip movieRip)
        {
            Movie relatedMovie = null;

            // vai ser ripReleaseDate == 0 e parsed == false sempre que movieRip.ParsedReleaseDate == null
            int ripReleaseDate;
            bool releaseDateParsed = Int32.TryParse(movieRip.ParsedReleaseDate, out ripReleaseDate);

            IEnumerable<string> ripTitleTokens = movieRip.GetParsedTitleTokens();
            IEnumerable<Movie> existingMatches = this._unitOfWork.Movies.Find(m => m.GetTitleTokens().SequenceEqual(ripTitleTokens));
            int matchCount = existingMatches.Count();
            if (matchCount == 1)
            {
                relatedMovie = existingMatches.First();
            }
            else if (matchCount > 1)
            {
                if (!releaseDateParsed)
                {
                    throw new MultipleMovieMatchesError(
                        $"Several matches in Movie repository for \"{movieRip.FileName}\" with Title = \"{movieRip.ParsedTitle}\"; count = {matchCount}"
                        );
                }
                else
                {
                    IEnumerable<Movie> existingMatchesWithDate = existingMatches.Where(m => m.ReleaseDate == ripReleaseDate);
                    int matchCountWithDate = existingMatchesWithDate.Count();
                    if (matchCountWithDate > 1)
                    {
                        throw new MultipleMovieMatchesError(
                            $"Several matches in Movie repository for \"{movieRip.FileName}\" with Title = \"{movieRip.ParsedTitle}\" and ReleaseDate = {ripReleaseDate}; count = {matchCount}"
                            );
                    }
                    else if (matchCountWithDate == 1)
                    {
                        relatedMovie = existingMatchesWithDate.First();
                    }
                }
            }

            return relatedMovie;
        }

        public void LinkMovieRipsToMovies()
        {

            IEnumerable<MovieRip> ripsToLink = GetMovieRipsToLink();

            List<string> errors = new();
            foreach (var movieRip in ripsToLink)
            {
                try
                {
                    Movie movie = FindRelatedMovieEntityInRepo(movieRip);
                    if (movie == null)
                    {

                    }
                    movieRip.Movie = movie;
                }
                // excepções lançadas na classe MovieFinder
                catch (Exception ex) when (ex is NoSearchResultsError || ex is MultipleSearchResultsError || ex is MultipleMovieMatchesError)
                {
                    var msg = $"Linking exception: {movieRip.FileName}: \n{ex.Message}";
                    errors.Add(msg);
                }
            }

            if (errors.Count() > 0)
            {
                string errorsFpath = Path.Combine(this._appSettingsManager.GetWarehouseContentsTextFilesDirectory(), $"linking_errors.txt");
                System.Console.WriteLine($"Erros no linking, consultar o seguinte ficheiro: {errorsFpath}");
                this._fileSystemIOWrapper.WriteAllText(errorsFpath, string.Join("\n\n", errors));
            }

            this._unitOfWork.Complete();
        }
    }

}