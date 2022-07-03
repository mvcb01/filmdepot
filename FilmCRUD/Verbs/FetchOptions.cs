using CommandLine;

namespace FilmCRUD.Verbs
{
    [Verb("fetch", HelpText = "fetch movie info online")]
    public class FetchOptions
    {
        [Option(SetName = "FetchGenres", HelpText = "fetch genres for movies")]
        public bool Genres { get; set; }

        [Option(SetName = "FetchActors", HelpText = "fetch actors for movies")]
        public bool Actors { get; set; }

        [Option(SetName = "FetchDirectors", HelpText = "fetch directors for movies")]
        public bool Directors { get; set; }

        [Option(SetName = "FetchKeywords", HelpText = "fetch keywords for movies")]
        public bool Keywords { get; set; }

        [Option(SetName = "FetchIMDBIds", HelpText = "fetch imdb ids for movies")]
        public bool IMDBIds { get; set; }
    }
}