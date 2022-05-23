using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

using FilmCRUD.CustomExceptions;
using FilmDomain.Entities;
using FilmDomain.Extensions;
using MovieAPIClients;
using MovieAPIClients.Interfaces;

namespace FilmCRUD.Helpers
{
    public class MovieFinder
    {
        private IMovieAPIClient _movieAPIClient { get; init; }

        public MovieFinder(IMovieAPIClient movieAPIClient)
        {
            this._movieAPIClient = movieAPIClient;
        }

        public async Task<Movie> FindMovieOnlineAsync(string parsedTitle, string parsedReleaseDate = null)
        {
            IEnumerable<MovieSearchResult> searchResultAll = await _movieAPIClient.SearchMovieAsync(parsedTitle);

            // filtra usando Title e OriginalTitle
            IEnumerable<string> titleTokens = parsedTitle.GetStringTokensWithoutPunctuation();
            List<MovieSearchResult> searchResult = searchResultAll
                .Where(r => titleTokens.SequenceEqual(r.Title.GetStringTokensWithoutPunctuation())
                    ||
                    titleTokens.SequenceEqual(r.OriginalTitle.GetStringTokensWithoutPunctuation()))
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
                int releaseDate;
                bool parseSuccess = int.TryParse(parsedReleaseDate, out releaseDate);
                string[] admissibleDates;
                if (parseSuccess)
                {
                    admissibleDates = new string[] {
                        releaseDate.ToString(),
                        (releaseDate + 1).ToString(),
                        (releaseDate - 1).ToString()
                    };
                }
                else
                {
                    admissibleDates = new string[] { parsedReleaseDate };
                }

                List<MovieSearchResult> searchResultFiltered = searchResult
                    .Where(r => admissibleDates.Contains(r.ReleaseDate.ToString()))
                    .ToList();
                int resultCountFiltered = searchResultFiltered.Count();

                if (resultCountFiltered == 0)
                {
                    throw new NoSearchResultsError(
                        $"No search results for \"{parsedTitle}\" with release date in {string.Join(", ", admissibleDates)}");
                }
                else if (resultCountFiltered > 1)
                {
                    throw new MultipleSearchResultsError(
                        $"Multiple search results for \"{parsedTitle}\"  with release date in {string.Join(", ", admissibleDates)}; count = {resultCount}");
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

    }

}
