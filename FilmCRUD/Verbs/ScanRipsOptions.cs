using CommandLine;
using System;
using System.Collections.Generic;

namespace FilmCRUD.Verbs
{
    // Examples:
    //  dotnet run -- scanrips --countbyreleasedate -v 20211206
    //  dotnet run -- scanrips --withdates 2011 2012 2013
    [Verb("scanrips", HelpText = "info from existing movie rips")]
    public class ScanRipsOptions
    {
        [Option(SetName = "CountRipsByReleaseDate", HelpText = "rip count by parsed release date for latest visit")]
        public bool CountByReleaseDate { get; set; }

        [Option(SetName = "GetRipsWithReleaseDates", HelpText = "list movies with parsed released dates YYYY")]
        public IEnumerable<int> WithDates { get; set; }

        [Option(SetName = "GetRipsGroup", HelpText = "list movies with parsed parsed release group")]
        public string WithGroup { get; set; }

        [Option(SetName = "CountRipsByVisit", HelpText = "rip count by visit")]
        public bool CountByVisit { get; set; }

        [Option(SetName = "LastVisitRipDifference", HelpText = "movie rip difference from last two visits: added and removed movie rips")]
        public bool LastVisitDiff { get; set; }

        [Option(SetName = "VisitRipDifference", Separator = ':',
            HelpText = "movie rip difference between two visits with dates YYYYMMDD: added and removed movie rips; example: 20100101:20100102")]
        public IEnumerable<string> VisitDiff { get; set; }

        [Option(SetName = "SearchOption", HelpText = "search movie rip filenames by tokens; examples: \"the.wicker.man.1973\", \"wicker man 1973\"")]
        public string Search { get; set; }

        [Option('l', "listvisits", SetName = "ListVisitsOption", HelpText = "helper; list dates for all visits")]
        public bool ListVisits { get; set; }

        // can be used with any set; relevant for options WithDates and CountByReleaseDate
        [Option('v', "visit", HelpText = "warehouse visit date (YYYYMMDD) to use as the scan target; defaults to the most recent visit")]
        public string Visit { get; set; }
    }
}