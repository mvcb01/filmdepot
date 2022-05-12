using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieCreditsResultTMDB
    {
        public class CastEach
        {
            [JsonPropertyName("id")]
            public int ExternalId { get; set; }


            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        public class CrewEach
        {
            [JsonPropertyName("id")]
            public int ExternalId { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("job")]
            public string Job { get; set; }
        }

        [JsonPropertyName("cast")]
        public IEnumerable<CastEach> Cast { get; set; }

        [JsonPropertyName("crew")]
        public IEnumerable<CrewEach> Crew { get; set; }

    }
}