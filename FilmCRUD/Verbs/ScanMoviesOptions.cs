using System.Collections.Generic;
using CommandLine;

namespace FilmCRUD.Verbs
{
    [Verb("scanmovies", HelpText = "info from existing movies")]
    public class ScanMoviesOptions
    {
        [Option(SetName = "WithGenreOptions", HelpText = "list movies with genres")]
        public IEnumerable<string> WithGenres { get; set; }

        [Option(SetName = "WithCastOptions", HelpText = "list movies with cast members")]
        public IEnumerable<string> WithCast { get; set; }

        [Option(SetName = "WithDirectorOptions", HelpText = "list movies with directors")]
        public IEnumerable<string> WithDirectors { get; set; }

        [Option(SetName = "WithDatesOptions", HelpText = "list movies with released dates")]
        public IEnumerable<int> WithDates { get; set; }

        [Option(SetName = "ByGenreOptions", HelpText = "get descending movie count by genre")]
        public bool ByGenre { get; set; }

        [Option(SetName = "ByCastMemberOptions", HelpText = "get descending movie count by cast member")]
        public bool ByCastMember { get; set; }

        [Option(SetName = "SearchOptions", HelpText = "search movies by title")]
        public string Search { get; set; }

        [Option(SetName = "ByDirectorOptions", HelpText = "get descending movie count by director")]
        public bool ByDirector { get; set; }

        [Option(SetName = "LastVisitMovieDifference", HelpText = "movie difference from last two visits")]
        public bool LastVisitDiff { get; set; }

        [Option(SetName = "VisitMovieDifference", Separator = ':',
            HelpText = "movie difference between two visits: added and removed; visit dates are expected; example: 20100101:20100102")]
        public IEnumerable<string> VisitDiff { get; set; }

        [Option(SetName = "ListVisitsOption", HelpText = "helper; list dates for all visits")]
        public bool ListVisits { get; set; }

        // can be used with any set, not relevant for ListVisitsOption
        [Option('v', "visit", HelpText = "warehouse visit date (YYYYMMDD) to use as the scan target; defaults to the most recent visit")]
        public string Visit { get; set; }

        // can be used with any set, only relevant for GetCountByGenre/CastMember/Director
        [Option('t', "top", HelpText = "integer to limit output count of bygenre/bycastmember/bydirector and get only the top N")]
        public int? Top { get; set; }

    }
}