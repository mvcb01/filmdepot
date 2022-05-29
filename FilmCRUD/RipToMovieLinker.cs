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

        private IMovieAPIClient _movieAPIClient { get; init; }

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
            this._movieAPIClient = movieAPIClient;
            this.MovieFinder = new MovieFinder(this._movieAPIClient);
        }

        /// <summary>
        /// Gets MovieRips not linked to a Movie, excluding RipFilenamesToIgnore and also those with ManualExternalIds
        /// </summary>
        public IEnumerable<MovieRip> GetMovieRipsToLink()
        {
            IEnumerable<string> toIgnore = _appSettingsManager.GetRipFilenamesToIgnoreOnLinking();
            Dictionary<string, int> manualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();
            IEnumerable<string> ripNamesToExclude = Enumerable.Concat<string>(toIgnore, manualExternalIds.Keys);
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

        public async Task SearchAndLinkAsync()
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

            PersistErrorInfo("linking_errors.txt", errors);

            _unitOfWork.Complete();
        }

        public async Task LinkFromManualExternalIdsAsync()
        {
            Dictionary<string, int> manualExternalIds = _appSettingsManager.GetManualExternalIds() ?? new Dictionary<string, int>();

            IEnumerable<MovieRip> ripsToLinkManually = _unitOfWork.MovieRips
                .Find(m => manualExternalIds.Keys.Contains(m.FileName));

            // Movie objects in repo that have one of the manually configured external ids
            IEnumerable<Movie> existingMoviesWithManualExternalIds = _unitOfWork.Movies
                .Find(m => manualExternalIds.Values.Contains(m.ExternalId));

            // we only need to manually link those MovieRip objects not yet linked or where the linked Movie.ExternalId does
            // not match the one given manually
            IEnumerable<MovieRip> ripsToLinkManuallyFiltered = ripsToLinkManually
                .Where(m => m.Movie == null || m.Movie.ExternalId != manualExternalIds[m.FileName]);

            Func<MovieRip, int, Task> getMovieInfoOnlineAndLinkAsync = async (movieRip, externalId) => {
                (string Title, string OriginalTitle, int ReleaseDate) movieInfo = await _movieAPIClient.GetMovieInfoAsync(externalId);
                movieRip.Movie = new Movie() {
                    ExternalId = externalId,
                    Title = movieInfo.Title,
                    OriginalTitle = movieInfo.OriginalTitle,
                    ReleaseDate = movieInfo.ReleaseDate
                };
            };

            List<Task> onlineInfoTasks = new();
            foreach (var movieRip in ripsToLinkManuallyFiltered)
            {
                int externalId = manualExternalIds[movieRip.FileName];
                Movie existingMovie = existingMoviesWithManualExternalIds
                    .Where(m => m.ExternalId == externalId).FirstOrDefault();
                if (existingMovie == null)
                {
                    Task onlineinfoTask = getMovieInfoOnlineAndLinkAsync(movieRip, externalId);
                    onlineInfoTasks.Add(onlineinfoTask);
                }
                else
                {
                    movieRip.Movie = existingMovie;
                }
            }

            await Task.WhenAll(onlineInfoTasks);
            _unitOfWork.Complete();
        }

        private void PersistErrorInfo(string filename, IEnumerable<string> errors)
        {
            if (errors.Count() == 0) { return; }

            string errorsFpath = Path.Combine(this._appSettingsManager.GetWarehouseContentsTextFilesDirectory(), filename);
            System.Console.WriteLine($"Erros no linking, consultar o seguinte ficheiro: {errorsFpath}");
            this._fileSystemIOWrapper.WriteAllText(errorsFpath, string.Join("\n\n", errors));
        }
    }

}