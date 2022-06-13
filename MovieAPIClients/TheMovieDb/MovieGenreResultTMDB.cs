using System.Text.Json.Serialization;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieGenreResultTMDB
    {
        [JsonPropertyName("id")]
        public int ExternalId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}