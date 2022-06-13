using System.Text.Json.Serialization;

namespace MovieAPIClients.TheMovieDb
{
    public class CrewResultTMDB
    {
        [JsonPropertyName("id")]
            public int ExternalId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("job")]
        public string Job { get; set; }

        // for explicit casts:
        //    var objTmdb = new CrewResultTMDB();
        //    var obj = (MovieDirectorResult)objTmdb;
        public static explicit operator MovieDirectorResult(CrewResultTMDB crewResultTMDB)
        {
            return new MovieDirectorResult() { ExternalId = crewResultTMDB.ExternalId, Name = crewResultTMDB.Name };
        }
    }
}