using System.Threading.Tasks;
using System.Collections.Generic;

namespace MovieAPIClients.Interfaces
{
    public interface IMovieAPIClient
    {
        Task<IEnumerable<MovieSearchResult>> SearchMovieAsync(string title);

        Task<IEnumerable<string>> GetMovieGenresAsync(int externalId);

        Task<IEnumerable<string>> GetMovieActorsAsync(int externalId);

        Task<IEnumerable<string>> GetMovieDirectorsAsync(int externalId);
    }
}