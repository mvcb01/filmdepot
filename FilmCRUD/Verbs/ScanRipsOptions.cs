using CommandLine;

namespace FilmCRUD.Verbs
{
    // Examples:
    //  dotnet run -- scan --countbyreleasedate
    //  dotnet run -- scan --withreleasedate 2011
    [Verb("scanrips", HelpText = "info from existing movie rips")]
    public class ScanRipsOptions
    {
        [Option(SetName = "CountRipsByReleaseDate", HelpText = "rip count by parsed release date for latest visit")]
        public bool CountByReleaseDate { get; set; }

        [Option(SetName = "GetRipsByReleaseDate", HelpText = "get all rips from last visit with releasedate YYYYMMDD")]
        public string WithReleaseDate { get; set; }

        [Option(SetName = "CountRipsByVisit", HelpText = "rip count by visit")]
        public bool CountByVisit { get; set; }

        [Option(SetName = "LastVisitRipDifference", HelpText = "movie rip difference from last two visits: added and removed")]
        public bool LastVisitDiff { get; set; }

        [Option(SetName = "ListVisitsOption", HelpText = "helper; list dates for all visits")]
        public bool ListVisits { get; set; }

        // can be used with any set, not relevant for CountRipsByVisit
        [Option('v', "visit", HelpText = "warehouse visit date (YYYY) to use as the scan target; defaults to the most recent visit")]
        public string Visit { get; set; }

    }
}