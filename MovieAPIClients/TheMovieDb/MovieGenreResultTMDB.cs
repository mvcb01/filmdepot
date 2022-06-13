using System.Text.Json.Serialization;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieGenreResultTMDB
    {
        [JsonPropertyName("id")]
        public int ExternalId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        // for explicit casts:
        //    var objTmdb = new MovieGenreResultTMDB();
        //    var obj = (MovieGenreResult)objTmdb;
        public static explicit operator MovieGenreResult(MovieGenreResultTMDB movieGenreResultTMDB)
        {
            return new MovieGenreResult() {
                ExternalId = movieGenreResultTMDB.ExternalId,
                Name = movieGenreResultTMDB.Name
                };
        }
    }
}