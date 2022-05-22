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

    }

}