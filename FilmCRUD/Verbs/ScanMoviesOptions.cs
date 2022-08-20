using System.Collections.Generic;
using CommandLine;

namespace FilmCRUD.Verbs
{
    [Verb("scanmovies", HelpText = "info from existing movies")]
    public class ScanMoviesOptions
    {
        [Option(SetName = "WithGenreOptions", HelpText = "list movies with genres")]
        public IEnumerable<string> WithGenres { get; set; }

        [Option(SetName = "WithActorOptions", HelpText = "list movies with actors")]
        public IEnumerable<string> WithActors { get; set; }

        [Option(SetName = "WithDirectorOptions", HelpText = "list movies with directors")]
        public IEnumerable<string> WithDirectors { get; set; }

        [Option(SetName = "WithDatesOptions", HelpText = "list movies with released dates")]
        public IEnumerable<int> WithDates { get; set; }

        [Option(SetName = "ByGenreOptions", HelpText = "get descending movie count by genre")]
        public bool ByGenre { get; set; }

        [Option(SetName = "ByActorOptions", HelpText = "get descending movie count by actor")]
        public bool ByActor { get; set; }

        [Option(SetName = "SearchOptions", HelpText = "search movies by title")]
        public string Search { get; set; }

        [Option(SetName = "ByDirectorOptions", HelpText = "get descending movie count by director")]
        public bool ByDirector { get; set; }

        [Option(SetName = "ListVisitsOption", HelpText = "helper; list dates for all visits")]
        public bool ListVisits { get; set; }

        // can be used with any set, not relevant for ListVisitsOption
        [Option('v', "visit", HelpText = "warehouse visit date (YYYY) to use as the scan target; defaults to the most recent visit")]
        public string Visit { get; set; }

        // can be used with any set, only relevant for GetCountByGenre/Actor/Director
        [Option('t', "top", HelpText = "integer to limit output count of bygenre/byactor/bydirector and get only the top N")]
        public int? Top { get; set; }


    }
}