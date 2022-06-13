using System.Text.Json.Serialization;

namespace MovieAPIClients.TheMovieDb
{
    public class CrewResultTMDB
    {
        [JsonPropertyName("id")]
            public int ExternalId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("job")]
        public string Job { get; set; }
    }
}