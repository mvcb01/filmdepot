using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using MovieAPIClients.Interfaces;

namespace MovieAPIClients.TheMovieDb
{
    public class TheMovieDbAPIClient : IMovieAPIClient
    {
        private string _apiKey { get; init; }

        private HttpClient _httpClient { get; init; }

        public const string MovieDbBaseAddress = "https://api.themoviedb.org/3/";

        public TheMovieDbAPIClient(string apiKey)
        {
            this._apiKey = apiKey;
            HttpClient client = new();
            client.BaseAddress = new Uri(MovieDbBaseAddress);
            this._httpClient = client;
        }

        public async Task<IEnumerable<MovieSearchResult>> SearchMovieAsync(string title)
        {
            var movieTitle = title.Trim().ToLower();

            // exemplo: converte "where, art thou!" para o array ["where", "art", "thou"]
            char[] punctuation = title.Where(Char.IsPunctuation).Distinct().ToArray();
            string[] titleWords = movieTitle.Split().Select(s => s.Trim(punctuation)).ToArray();

            // para o search da query string, por exemplo
            //      query=where+art+thou
            string searchQuery = string.Join('+', titleWords);

            // por enquanto fica martelada a primeira página no fim da query string
            string resultString = await _httpClient.GetStringAsync($"search/movie?api_key={_apiKey}&query={searchQuery}&page=1");

            var searchResultTMDB = JsonSerializer.Deserialize<SearchResultTMDB>(resultString);
            return searchResultTMDB.Results.Select(res => res.ToMovieSearchResult());
        }

        public async Task<IEnumerable<string>> GetMovieGenresAsync(int externalId)
        {
            // vamos buscar aos movie details
            string resultString = await _httpClient.GetStringAsync($"movie/{externalId}?api_key={_apiKey}");
            var resultObj = JsonSerializer.Deserialize<MovieGenresResultTMDB>(resultString);
            return resultObj.Genres.Select(g => g.Name);
        }

        public async Task<IEnumerable<string>> GetMovieActorsAsync(int externalId)
        {
            // vamos buscar aos movie credits
            string resultString = await _httpClient.GetStringAsync($"movie/{externalId}/credits?api_key={_apiKey}");
            var resultObj = JsonSerializer.Deserialize<MovieCreditsResultTMDB>(resultString);
            return resultObj.Cast.Select(c => c.Name);
        }

        public async Task<IEnumerable<string>> GetMovieDirectorsAsync(int externalId)
        {
            // vamos buscar aos movie credits
            string resultString = await _httpClient.GetStringAsync($"movie/{externalId}/credits?api_key={_apiKey}");
            var resultObj = JsonSerializer.Deserialize<MovieCreditsResultTMDB>(resultString);
            return resultObj.Crew.Where(c => c.Job.Trim().ToLower() == "director").Select(c => c.Name);
        }

    }
}
