using System.Threading.Tasks;
using System.Collections.Generic;

namespace MovieAPIClients.Interfaces
{
    public interface IMovieAPIClient
    {
        string ApiBaseAddress { get; }

        Task<IEnumerable<MovieSearchResult>> SearchMovieAsync(string title);

        Task<bool> ExternalIdExistsAsync(int externalId);

        Task<string> GetMovieTitleAsync(int externalId);

        Task<string> GetMovieOriginalTitleAsync(int externalId);

        Task<int> GetMovieReleaseDateAsync(int externalId);

        Task<MovieSearchResult> GetMovieInfoAsync(int externalId);

        Task<string> GetMovieIMDBIdAsync(int externalId);

        Task<IEnumerable<string>> GetMovieKeywordsAsync(int externalId);

        Task<IEnumerable<MovieGenreResult>> GetMovieGenresAsync(int externalId);

        Task<IEnumerable<MovieActorResult>> GetMovieActorsAsync(int externalId);

        Task<IEnumerable<MovieDirectorResult>> GetMovieDirectorsAsync(int externalId);
    }
}