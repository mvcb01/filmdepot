using System.Text.Json.Serialization;
using System;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieSearchResultTMDB
    {
        [JsonPropertyName("id")]
        public int ExternalId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("original_title")]
        public string OriginalTitle { get; set; }

        [JsonPropertyName("release_date")]
        public string ReleaseDateString { get; set; }

        // release_date comes in format YYYY-mm-dd
        public int ReleaseDate
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(ReleaseDateString, "yyyy-MM-dd", null).Year;
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException)
                {

                    return 0;
                }

            }
        }

    }
}