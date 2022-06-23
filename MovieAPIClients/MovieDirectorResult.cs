using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace MovieAPIClients
{
    public class MovieDirectorResult : IExternalEntity
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