using FilmDomain.Entities;

namespace MovieAPIClients
{
    public class MovieDirectorResult
    {
        public int ExternalId { get; set; }

        public string Name { get; set; }

        public static explicit operator Director(MovieDirectorResult movieDirectorResult)
        {
            return new Director() {
                ExternalId = movieDirectorResult.ExternalId,
                Name = movieDirectorResult.Name
            };
        }
    }
}