namespace MovieAPIClients.TheMovieDb
{
    public class TheMovieDbAPIClient
    {
        private string _apiKey { get; init; }

        private static readonly HttpClient client = new HttpClient();

        public const string MovieDbBaseAddress = "https://api.themoviedb.org/3/";

        public TheMovieDbAPIClient(string apiKey)
        {
            this._apiKey = apiKey;
        }

    }
}