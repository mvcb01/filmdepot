using CommandLine;

namespace FilmCRUD.Verbs
{

    [Verb("link", HelpText = "Para ligar movie rips a filmes e pesquisas online")]
    public class LinkOptions
    {

        [Option(SetName = "SearchLocallyAndOnline", HelpText = "Procura localmente e online")]
        public bool Search { get; set; }

        [Option(SetName = "LinkFromManualExternalIds", HelpText = "Procura online a partir dos external ids manuais")]
        public bool FromManualExtIds { get; set; }

        [Option(SetName = "GetUnlinkedMovieRips", HelpText = "nomes dos movierips não linkados")]
        public bool GetUnlinkedRips { get; set; }

        [Option(SetName = "ValidateManualExternalIds", HelpText = "valida os external ids manuais")]
        public bool ValidateManualExtIds { get; set; }

    }

}