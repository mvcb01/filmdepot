using System.Collections.Generic;
using CommandLine;

namespace FilmCRUD.Verbs
{
    [Verb("scanmovies", HelpText = "info from existing movies")]
    public class ScanMoviesOptions
    {
        [Option(SetName = "GenreOptions", HelpText = "")]
        public IEnumerable<string> Genres { get; set; }

        // can be used with any set
        // [Option()]
        // public string VisitDate { get; set; }
    }
}