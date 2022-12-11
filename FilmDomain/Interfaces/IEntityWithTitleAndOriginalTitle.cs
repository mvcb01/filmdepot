using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilmDomain.Interfaces
{
    public interface IEntityWithTitleAndOriginalTitle
    {
        public string Title { get; set; }

        public string OriginalTitle { get; set; }
    }
}
