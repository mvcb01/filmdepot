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

        // can be used with any set
        [Option('v', "visit", HelpText = "warehouse visit to use as the scan target; defaults to the most recent visit")]
        public string Visit { get; set; }

        [Option(SetName = "ListVisitsOption", HelpText = "helper; list dates for all visits")]
        public bool ListVisits { get; set; }
    }
}