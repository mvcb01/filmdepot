using System;
using System.Net.Http;

namespace MovieAPIClients.TheMovieDb
{
    public class TheMovieDbAPIClientRateLimited
    {
        private string _apiKey { get; init; }

        private HttpClient _httpClient { get; init; }

        public const string MovieDbBaseAddress = "https://api.themoviedb.org/3/";

        public TheMovieDbAPIClientRateLimited(string apiKey)
        {
            this._apiKey = apiKey;
            HttpClient client = new();
            client.BaseAddress = new Uri(MovieDbBaseAddress);
            this._httpClient = client;
        }
    }
}