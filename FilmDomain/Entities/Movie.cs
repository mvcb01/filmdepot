using FilmDomain.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FilmDomain.Entities
{
    public class Movie : IEntityWithTitleAndOriginalTitle
    {
        public int Id { get; set; }

        public int ExternalId { get; set; }

        public string Title { get; set; }

        public string OriginalTitle { get; set; }

        public int ReleaseDate { get; set; }

        public string IMDBId { get; set; }

        public ICollection<string> Keywords { get; set; }

        public ICollection<Genre> Genres { get; set; }

        public ICollection<Director> Directors { get; set; }

        public ICollection<MovieRip> MovieRips { get; set; }

        public ICollection<CastMember> CastMembers { get; set; }

        public override string ToString()
        {
            return $"{Title} ({ReleaseDate})";
        }

    }
}