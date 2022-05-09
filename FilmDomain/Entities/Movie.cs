using System.Collections.Generic;
using System.Linq;

namespace FilmDomain.Entities
{
    public class Movie
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public int ReleaseDate { get; set; }

        public ICollection<Genre> Genres { get; set; }

        public ICollection<Director> Directors { get; set; }

        public ICollection<MovieRip> MovieRips { get; set; }

        public ICollection<Actor> Actors { get; set; }

        public override string ToString()
        {
            string _genres = "";
            if (Genres != null)
            {
                _genres = string.Join(' ', Genres.Select(g => g.Name));
            }
            return $"{Title} ({ReleaseDate}) - {_genres}";
        }
    }
}