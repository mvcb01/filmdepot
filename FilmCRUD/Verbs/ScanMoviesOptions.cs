using System.Collections.Generic;
using CommandLine;

namespace FilmCRUD.Verbs
{
    [Verb("scanmovies", HelpText = "info from existing movies")]
    public class ScanMoviesOptions
    {
        [Option(SetName = "GenreOptions", HelpText = "list movies with genres")]
        public IEnumerable<string> WithGenres { get; set; }

        // can be used with any set
        [Option('v', "visit")]
        public string Visit { get; set; }
    }
}