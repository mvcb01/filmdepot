using System.Text.Json.Serialization;

namespace MovieAPIClients.TheMovieDb
{
    public class CastResultTMDB
    {
        [JsonPropertyName("id")]
        public int ExternalId { get; set; }


        [JsonPropertyName("name")]
        public string Name { get; set; }

        // for explicit casts:
        //    var objTmdb = new CastResultTMDB();
        //    var obj = (MovieActorResult)objTmdb;
        public static explicit operator MovieActorResult(CastResultTMDB castResultTMDB)
        {
            return new MovieActorResult() { ExternalId = castResultTMDB.ExternalId, Name = castResultTMDB.Name };
        }
    }
}