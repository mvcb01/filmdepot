using FilmDomain.Entities;

namespace MovieAPIClients
{
    public class MovieGenreResult
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