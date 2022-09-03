using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using Polly;
using Polly.Retry;
using Polly.RateLimit;
using System.Threading;

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

        public async Task<IEnumerable<MovieGenreResult>> GetMovieGenresAsync(int externalId)
        {
            string resultString = await _httpClient.GetStringAsync($"movie/{externalId}?api_key={_apiKey}");
            return JsonSerializer.Deserialize<MovieGenresResultTMDB>(resultString).Genres.Select(g => (MovieGenreResult)g);
        }

        public async Task<Tuple<string, string>> GetMovieInfoAsync(int externalId)
        {
            var resultString = await _httpClient.GetStringAsync($"movie/{externalId}?api_key={_apiKey}");
            var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(resultString);
            (string, string) result = (
                resultDict["title"].ToString(),
                resultDict["release_date"].ToString()
            );
            return result.ToTuple<string, string>();
        }

        public async Task<string> DummyExecutionsAsync()
        {
            AsyncRateLimitPolicy<string> limitPolicy = GetRateLimitPolicy<string>(1, TimeSpan.FromSeconds(1));

            // AsyncRateLimitPolicy limitPolicy = Policy.RateLimitAsync(1, TimeSpan.FromSeconds(1));

            Func<int, Task<string>> f = async i => {
                await Task.Delay(100);
                return $"iter {i}";
            };

            List<string> resultList = new();
            Dictionary<string, TimeSpan> retryInfo = new();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    string output = await limitPolicy.ExecuteAsync(() => f(i));
                    resultList.Add(output);
                }
                catch (RateLimitRejectedException ex)
                {
                    var msg = ex.Message;
                    TimeSpan retryAfter = ex.RetryAfter;
                    retryInfo.Add(i.ToString(), retryAfter);
                }

            }


            return string.Join(" | ", resultList);
        }

        public async Task<IEnumerable<MovieGenreResult>> GetMovieGenresWithRetryAsync(int externalId)
        {
            AsyncRetryPolicy retryPolicy = GetAsyncRetryPolicy();

            Func<Task<string>> f = () => _httpClient.GetStringAsync($"movie/{externalId}?api_key={_apiKey}");
            string resultString = await retryPolicy.ExecuteAsync<string>(f);

            return JsonSerializer.Deserialize<MovieGenresResultTMDB>(resultString).Genres.Select(g => (MovieGenreResult)g);
        }

        private static AsyncRetryPolicy GetAsyncRetryPolicy()
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>(exc => exc.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(4),
                    });
            return retryPolicy;
        }

        private static AsyncRateLimitPolicy<TResult> GetRateLimitPolicy<TResult>(
            int numberOfExecutions,
            TimeSpan perTimeSpan)
        {
            return Policy.RateLimitAsync<TResult>(
                numberOfExecutions,
                perTimeSpan

                );

        }
    }
}