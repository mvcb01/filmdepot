using CommandLine;

namespace FilmCRUD.Verbs
{
    // Examples:
    //  dotnet run -- visit --listcontents
    //  dotnet run -- visit --persistcontents 20220306
    [Verb("visit", HelpText = "to generate the warehouse contents text file and persist contents in repo")]
    public class VisitOptions
    {
        [Option(SetName = "ListingContents", HelpText = "generate the warehouse contents text file using the configured paths")]
        public bool ListContents { get; set; }

        [Option(
            SetName = "PersistingContents",
            HelpText = "read the warehouse contents textfile with the provided date and persist in repo")]
        public string PersistContents { get; set; }

        [Option(
            SetName = "ReprocessingExistingVisit",
            HelpText = "process the manuallly configured movie rips for an existing visit; a visit date is expected")]
        public string ProcessManual { get; set; }

        [Option('l', "listvisits", SetName = "ListVisitsOption", HelpText = "helper; list dates for all past visits")]
        public bool ListVisits { get; set; }
    }
}