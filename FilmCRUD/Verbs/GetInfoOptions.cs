using CommandLine;

namespace FilmCRUD.Verbs
{
    [Verb("getinfo", HelpText = "get movie info online")]
    public class GetInfoOptions
    {
        [Option(SetName = "GetGenres", HelpText = "get genres for movies")]
        public bool Genres { get; set; }

        [Option(SetName = "GetActors", HelpText = "get actors for movies")]
        public bool Actors { get; set; }

        [Option(SetName = "GetDirectors", HelpText = "get directors for movies")]
        public bool Directors { get; set; }

        [Option(SetName = "GetKeywords", HelpText = "get keywords for movies")]
        public bool Keywords { get; set; }

        //
        [Option(SetName = "GetIMDBIds", HelpText = "get imdb ids for movies")]
        public bool IMDBIds { get; set; }
    }
}