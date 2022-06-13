using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieGenresResultTMDB
    {
        [JsonPropertyName("genres")]
        public IEnumerable<MovieGenreResultTMDB> Genres { get; set; }
    }
}