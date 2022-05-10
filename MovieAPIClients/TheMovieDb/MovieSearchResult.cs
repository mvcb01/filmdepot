using System.Text.Json.Serialization;
using System;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieSearchResult
    {
        [JsonPropertyName("id")]
        public int ExternalId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("original_title")]
        public string OriginalTitle { get; set; }

        [JsonPropertyName("release_date")]
        public string ReleaseDateString { get; set; }

        // a release_date vem na forma 2021-03-17
        public int ReleaseDate
        {
            get { return DateTime.ParseExact(ReleaseDateString, "yyyy-MM-dd", null).Year; }
        }

    }
}