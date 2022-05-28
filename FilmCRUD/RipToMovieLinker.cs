using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using ConfigUtils.Interfaces;
using FilmCRUD.Helpers;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Interfaces;
using MovieAPIClients.Interfaces;


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

        /// <summary>
        /// Gets MovieRips not linked to a Movie, excluding RipFilenamesToIgnore and also those with ManualExternalIds
        /// </summary>
        public IEnumerable<MovieRip> GetMovieRipsToLink()
        {
            IEnumerable<string> toIgnore = _appSettingsManager.GetRipFilenamesToIgnoreOnLinking();
            Dictionary<string, int> externalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();
            IEnumerable<string> ripNamesToExclude = Enumerable.Concat<string>(toIgnore, externalIds.Keys);
            return _unitOfWork.MovieRips
                .Find(r => r.Movie == null)
                .Where(r => r.ParsedTitle != null && !ripNamesToExclude.Contains(r.FileName));
        }

        public Movie FindRelatedMovieEntityInRepo(MovieRip movieRip)
        {

            Movie relatedMovie = null;

            // vai ser ripReleaseDate == 0 e parsed == false sempre que movieRip.ParsedReleaseDate == null
            int ripReleaseDate;
            bool releaseDateParsed = Int32.TryParse(movieRip.ParsedReleaseDate, out ripReleaseDate);

            IEnumerable<Movie> existingMatches = this._unitOfWork.Movies.SearchMoviesWithTitle(movieRip.ParsedTitle);
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

        public async Task SearchAndLinkMovieRipsToMoviesAsync()
        {

            IEnumerable<MovieRip> ripsToLink = GetMovieRipsToLink();

            Func<MovieRip, Task> linkRipToOnlineSearchAsync = async (MovieRip movieRip) => {
                movieRip.Movie = await this.MovieFinder.FindMovieOnlineAsync(
                    movieRip.ParsedTitle,
                    movieRip.ParsedReleaseDate
                    );
            };

            List<string> errors = new();
            List<Task> onlineSearches = new();
            foreach (var movieRip in ripsToLink)
            {
                try
                {
                    Movie movie = FindRelatedMovieEntityInRepo(movieRip);
                    if (movie != null)
                    {
                        movieRip.Movie = movie;

                    }
                    else
                    {
                        Task onlineSearch = linkRipToOnlineSearchAsync(movieRip);
                        onlineSearches.Add(onlineSearch);
                    }

                }
                // excepções lançadas no método FindRelatedMovieEntityInRepo
                catch (MultipleMovieMatchesError ex)
                {
                    var msg = $"MultipleMovieMatchesError: {movieRip.FileName}: \n{ex.Message}";
                    errors.Add(msg);
                }
            }

            try
            {
                await Task.WhenAll(onlineSearches);
            }
            // excepções lançadas no método MovieFinder.FindMovieOnlineAsync
            catch (Exception ex) when (ex is NoSearchResultsError || ex is MultipleSearchResultsError)
            {}

            foreach (Task task in onlineSearches.Where(t => !t.IsCompletedSuccessfully))
            {
                Exception innerExc = task.Exception.InnerException;
                errors.Add($"ex message: {innerExc.GetType().Name}: {innerExc.Message}");
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