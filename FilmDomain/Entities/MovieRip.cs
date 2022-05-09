using System.Collections.Generic;

namespace FilmDomain.Entities
{
    public class MovieRip
    {
        public int Id { get; set; }

        public string FileName { get; set; }

        public string ParsedTitle { get; set; }

        public string ParsedReleaseDate { get; set; }

        public string ParsedRipQuality { get; set; }

        public string ParsedRipInfo { get; set; }

        public string ParsedRipGroup { get; set; }

        public Movie Movie { get; set; }

        public ICollection<MovieWarehouseVisit> MovieWarehouseVisits { get; set; }

        public override string ToString()
        {
            return "MovieRip: " + FileName;
        }

    }
}