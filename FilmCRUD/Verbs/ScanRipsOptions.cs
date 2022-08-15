using CommandLine;
using System.Collections.Generic;

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

        [Option(SetName = "GetRipsWithReleaseDates", HelpText = "list movies with parsed released dates YYYY")]
        public IEnumerable<int> WithDates { get; set; }

        [Option(SetName = "CountRipsByVisit", HelpText = "rip count by visit")]
        public bool CountByVisit { get; set; }

        [Option(SetName = "LastVisitRipDifference", HelpText = "movie rip difference from last two visits: added and removed")]
        public bool LastVisitDiff { get; set; }

    }
}