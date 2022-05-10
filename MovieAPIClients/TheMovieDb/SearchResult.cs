using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MovieAPIClients.TheMovieDb
{
    public class SearchResult
    {
        [JsonPropertyName("results")]
        public IEnumerable<MovieSearchResult> Results { get; set; }
    }
}