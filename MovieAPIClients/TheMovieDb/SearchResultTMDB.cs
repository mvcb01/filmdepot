using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MovieAPIClients.TheMovieDb
{
    public class SearchResultTMDB
    {
        [JsonPropertyName("results")]
        public IEnumerable<MovieSearchResultTMDB> Results { get; set; }
    }
}