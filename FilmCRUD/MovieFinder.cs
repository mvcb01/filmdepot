using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

using FilmCRUD.CustomExceptions;
using FilmDomain.Entities;
using MovieAPIClients;
using ConfigUtils.Interfaces;
using MovieAPIClients.Interfaces;
using FilmDomain.Interfaces;

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
            IEnumerable<MovieSearchResult> searchResultAll = await _movieAPIClient.SearchMovieAsync(parsedTitle);

            // filtra usando Title e OriginalTitle
            IEnumerable<string> titleTokens = SplitTitleIntoTokens(parsedTitle);
            List<MovieSearchResult> searchResult = searchResultAll
                .Where(r => titleTokens.SequenceEqual(SplitTitleIntoTokens(r.Title ?? string.Empty))
                    ||
                    titleTokens.SequenceEqual(SplitTitleIntoTokens(r.OriginalTitle ?? string.Empty)))
                .ToList();

            int resultCount = searchResult.Count();
            MovieSearchResult result;
            if (resultCount == 0)
            {
                throw new NoSearchResultsError($"No search results for \"{parsedTitle}\" ");
            }
            else if (resultCount == 1)
            {
                result = searchResult.First();
            }
            else if (parsedReleaseDate == null)
            {
                throw new MultipleSearchResultsError($"Multiple search results for \"{parsedTitle}\"; count = {resultCount}");
            }
            else
            {
                List<MovieSearchResult> searchResultFiltered = searchResult
                    .Where(r => r.ReleaseDate.ToString() == parsedReleaseDate.Trim())
                    .ToList();
                int resultCountFiltered = searchResultFiltered.Count();

                if (resultCountFiltered == 0)
                {
                    throw new NoSearchResultsError(
                        $"No search results for \"{parsedTitle}\" with release date = {parsedReleaseDate}");
                }
                else if (resultCountFiltered > 1)
                {
                    throw new MultipleSearchResultsError(
                        $"Multiple search results for \"{parsedTitle}\"  with release date = {parsedReleaseDate}; count = {resultCount}");
                }
                result = searchResultFiltered.First();
            }

            return new Movie() {
                Title = result.Title,
                ExternalId = result.ExternalId,
                OriginalTitle = result.OriginalTitle,
                ReleaseDate = result.ReleaseDate
                };
        }

        private static IEnumerable<string> SplitTitleIntoTokens(string parsedTitle)
        {
            var movieTitle = parsedTitle.Trim().ToLower();
            char[] punctuation = movieTitle.Where(Char.IsPunctuation).Distinct().ToArray();
            return movieTitle.Split().Select(s => s.Trim(punctuation));
        }
    }

}