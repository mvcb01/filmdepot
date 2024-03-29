using CommandLine;

namespace FilmCRUD.Verbs
{
    [Verb("fetch", HelpText = "fetch movie info online")]
    public class FetchOptions
    {
        [Option(SetName = "FetchGenres", HelpText = "fetch genres for movies")]
        public bool Genres { get; set; }

        [Option(SetName = "FetchCastMembers", HelpText = "fetch cast members for movies")]
        public bool CastMembers { get; set; }

        [Option(SetName = "FetchDirectors", HelpText = "fetch directors for movies")]
        public bool Directors { get; set; }

        [Option(SetName = "FetchKeywords", HelpText = "fetch keywords for movies")]
        public bool Keywords { get; set; }

        [Option(SetName = "FetchIMDBIds", HelpText = "fetch imdb ids for movies")]
        public bool IMDBIds { get; set; }

        [Option('m', "maxcalls", HelpText = "optional integer to limit the number of API calls")]
        public int? MaxCalls { get; set; }
    }
}