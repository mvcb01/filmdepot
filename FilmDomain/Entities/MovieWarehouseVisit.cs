using System;
using System.Collections.Generic;

namespace FilmDomain.Entities
{
    public class MovieWarehouseVisit
    {
        public int Id { get; set; }

        public DateTime VisitDateTime { get; set; }

        public ICollection<MovieRip> MovieRips { get; set; }
    }
}