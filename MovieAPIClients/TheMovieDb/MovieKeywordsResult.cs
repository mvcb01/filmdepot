using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieKeywordsResult
    {
        public class Keyword
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        [JsonPropertyName("id")]
        public int ExternalId { get; set; }

        [JsonPropertyName("keywords")]
        public IEnumerable<Keyword> Keywords { get; set; }


    }
}