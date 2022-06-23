using System.Collections.Generic;
using FilmDomain.Interfaces;

namespace FilmDomain.Entities
{
    public class Director : IExternalEntity
    {
        public int Id { get; set; }

        public int ExternalId { get; set; }

        public string Name { get; set; }

        public ICollection<Movie> Movies { get; set; }
    }
}