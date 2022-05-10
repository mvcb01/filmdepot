using System.Net.Http;

namespace MovieAPIClients.TheMovieDb
{
    public class TheMovieDbAPIClient
    {
        private string _apiKey { get; init; }

        private HttpClient _httpClient { get; init; }

        public const string MovieDbBaseAddress = "https://api.themoviedb.org/3/";

        public TheMovieDbAPIClient(string apiKey)
        {
            this._apiKey = apiKey;
            this._httpClient = new HttpClient();
        }

    }
}