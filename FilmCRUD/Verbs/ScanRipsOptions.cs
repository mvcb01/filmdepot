using CommandLine;

namespace FilmCRUD.Verbs
{
    // Exemplos:
    //  dotnet run -- scan --countbyreleasedate
    //  dotnet run -- scan --withreleasedate 2011
    [Verb("scanrips", HelpText = "Informação dos rips existentes")]
    public class ScanRipsOptions
    {
        [Option(SetName = "CountRipsByReleaseDate", HelpText = "Contagem por release date")]
        public bool CountByReleaseDate { get; set; }

        [Option(SetName = "GetRipsByReleaseDate", HelpText = "Rips por release date")]
        public string WithReleaseDate { get; set; }

        [Option(SetName = "CountRipsByVisit", HelpText = "Contagem por visita")]
        public bool CountByVisit { get; set; }

        [Option(SetName = "LastVisitRipDifference", HelpText = "Novos rips e rips removidos na última visita")]
        public bool LastVisitDiff { get; set; }

    }
}