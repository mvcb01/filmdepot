using System.Collections.Generic;

namespace FilmDomain.Entities
{
    public class Actor
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<Movie> Movies { get; set; }
    }
}