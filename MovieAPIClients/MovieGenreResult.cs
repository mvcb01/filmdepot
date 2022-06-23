using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace MovieAPIClients
{
    public class MovieGenreResult : IExternalEntity
    {
        public int ExternalId { get; set; }

        public string Name { get; set; }

        public static explicit operator Genre(MovieGenreResult movieGenreResult)
        {
            return new Genre() {
                ExternalId = movieGenreResult.ExternalId,
                Name = movieGenreResult.Name
            };
        }
    }
}