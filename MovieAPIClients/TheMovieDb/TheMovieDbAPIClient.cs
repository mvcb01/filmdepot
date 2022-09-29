using System;
using System.Net;
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
            return searchResultTMDB.Results.Select(res => (MovieSearchResult)res);
        }

        public async Task<bool> ExternalIdExistsAsync(int externalId)
        {
            bool exists;
            HttpResponseMessage result = await _httpClient.GetAsync($"movie/{externalId}?api_key={_apiKey}");
            if (result.IsSuccessStatusCode)
            {
                exists = true;
            }
            else
            {
                HttpStatusCode code = result.StatusCode;
                if (code != HttpStatusCode.NotFound)
                {
                    string msg = $"Error when calling TheMovieDbAPIClient.ExternalIdExists with externalId = {externalId};" +
                        $" StatusCode: {code}, Message: {result.ReasonPhrase}";
                    throw new HttpRequestException(msg);
                }
                exists = false;
            }
            return exists;
        }

        public async Task<string> GetMovieTitleAsync(int externalId)
        {
            var resultObj = await GetMovieDetailsFromExternalIdAsync<MovieSearchResultTMDB>(externalId);
            return resultObj.Title;
        }

        public async Task<string> GetMovieOriginalTitleAsync(int externalId)
        {
            var resultObj = await GetMovieDetailsFromExternalIdAsync<MovieSearchResultTMDB>(externalId);
            return resultObj.OriginalTitle;
        }

        public async Task<int> GetMovieReleaseDateAsync(int externalId)
        {
            var resultObj = await GetMovieDetailsFromExternalIdAsync<MovieSearchResultTMDB>(externalId);
            return resultObj.ReleaseDate;
        }

        public async Task<MovieSearchResult> GetMovieInfoAsync(int externalId)
        {
            var resultObj = await GetMovieDetailsFromExternalIdAsync<MovieSearchResultTMDB>(externalId);
            return (MovieSearchResult)resultObj;
        }

        public async Task<string> GetMovieIMDBIdAsync(int externalId)
        {
            // too simple to create a new class just to get the result...
            var resultDict = await GetMovieDetailsFromExternalIdAsync<Dictionary<string, object>>(externalId);
            return resultDict["imdb_id"].ToString();
        }

        public async Task<IEnumerable<string>> GetMovieKeywordsAsync(int externalId)
        {
            string resultString = await _httpClient.GetStringAsync($"movie/{externalId}/keywords?api_key={_apiKey}");
            var resultObj = JsonSerializer.Deserialize<MovieKeywordsResult>(resultString);
            return resultObj.Keywords.Select(k => k.Name);
        }

        public async Task<IEnumerable<MovieGenreResult>> GetMovieGenresAsync(int externalId)
        {
            var resultObj = await GetMovieDetailsFromExternalIdAsync<MovieGenresResultTMDB>(externalId);
            return resultObj.Genres.Select(g => (MovieGenreResult)g);
        }

        public async Task<IEnumerable<MovieActorResult>> GetMovieActorsAsync(int externalId)
        {
            MovieCreditsResultTMDB resultObj = await GetMovieCreditsFromExternalIdAsync(externalId);
            return resultObj.Cast.Select(c => (MovieActorResult)c);
        }

        public async Task<IEnumerable<MovieDirectorResult>> GetMovieDirectorsAsync(int externalId)
        {
            MovieCreditsResultTMDB resultObj = await GetMovieCreditsFromExternalIdAsync(externalId);
            return resultObj.Crew.Where(c => c.Job.Trim().ToLower() == "director").Select(c => (MovieDirectorResult)c);
        }

        private async Task<T> GetMovieDetailsFromExternalIdAsync<T>(int externalId)
        {
            string resultString = await _httpClient.GetStringAsync($"movie/{externalId}?api_key={_apiKey}");
            return JsonSerializer.Deserialize<T>(resultString);
        }

        private async Task<MovieCreditsResultTMDB> GetMovieCreditsFromExternalIdAsync(int externalId)
        {
            string resultString = await _httpClient.GetStringAsync($"movie/{externalId}/credits?api_key={_apiKey}");
            return JsonSerializer.Deserialize<MovieCreditsResultTMDB>(resultString);
        }

    }
}
