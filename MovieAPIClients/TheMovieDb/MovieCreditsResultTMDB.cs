using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MovieAPIClients.TheMovieDb
{
    public class MovieCreditsResultTMDB
    {
        [JsonPropertyName("cast")]
        public IEnumerable<CastResultTMDB> Cast { get; set; }

        [JsonPropertyName("crew")]
        public IEnumerable<CrewResultTMDB> Crew { get; set; }

    }
}