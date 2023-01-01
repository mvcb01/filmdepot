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
        public static explicit operator MovieCastMemberResult(CastResultTMDB castResultTMDB)
        {
            return new MovieCastMemberResult() { ExternalId = castResultTMDB.ExternalId, Name = castResultTMDB.Name };
        }
    }
}