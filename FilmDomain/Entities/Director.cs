using System.Collections.Generic;

namespace FilmDomain.Entities
{
    public class Director
    {
        public int Id { get; set; }

        public int ExternalId { get; set; }

        public string Name { get; set; }

        public ICollection<Movie> Movies { get; set; }
    }
}