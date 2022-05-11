using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieGenresResultTMDB
    {
        public class GenreEach
        {
            [JsonPropertyName("id")]
            public int ExternalId { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        [JsonPropertyName("genres")]
        public IEnumerable<GenreEach> Genres { get; set; }
    }
}