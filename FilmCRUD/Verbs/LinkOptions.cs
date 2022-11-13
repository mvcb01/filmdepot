using CommandLine;
using CommandLine.Text;

namespace FilmCRUD.Verbs
{
    [Verb("link", HelpText = "to link each movierip to a movie using online and local searches")]
    public class LinkOptions
    {
        [Option(SetName = "SearchLocallyAndOnline", HelpText = "search locally and online")]
        public bool Search { get; set; }

        [Option(SetName = "LinkFromManualExternalIds", HelpText = "link using the manually configured external ids")]
        public bool FromManualExtIds { get; set; }

        [Option(SetName = "GetUnlinkedMovieRips", HelpText = "get all movierips not linked to a movie")]
        public bool GetUnlinkedRips { get; set; }

        [Option(SetName = "ValidateManualExternalIds", HelpText = "validate the manually configured external ids")]
        public bool ValidateManualExtIds { get; set; }

        [Option('m', "maxcalls", HelpText = "optional integer to limit the number of API calls")]
        public int MaxCalls { get; set; }
    }
}