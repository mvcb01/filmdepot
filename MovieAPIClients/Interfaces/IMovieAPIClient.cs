using System.Threading.Tasks;
using System.Collections.Generic;

namespace MovieAPIClients.Interfaces
{
    public interface IMovieAPIClient
    {
        Task<IEnumerable<MovieSearchResult>> SearchMovieAsync(string title);

        Task<bool> ExternalIdExistsAsync(int externalId);

        Task<string> GetMovieTitleAsync(int externalId);

        Task<string> GetMovieOriginalTitleAsync(int externalId);

        Task<int> GetMovieReleaseDateAsync(int externalId);

        Task<(string Title, string OriginalTitle, int ReleaseDate)> GetMovieInfoAsync(int externalId);

        Task<string> GetMovieIMDBIdAsync(int externalId);

        Task<IEnumerable<string>> GetMovieKeywordsAsync(int externalId);

        Task<IEnumerable<string>> GetMovieGenresAsync(int externalId);

        Task<IEnumerable<string>> GetMovieActorsAsync(int externalId);

        Task<IEnumerable<string>> GetMovieDirectorsAsync(int externalId);
    }
}