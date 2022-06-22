using FilmDomain.Entities;

namespace MovieAPIClients
{
    public class MovieActorResult
    {
        public int ExternalId { get; set; }

        public string Name { get; set; }

        public static explicit operator Actor(MovieActorResult movieActorResult)
        {
            return new Actor() {
                ExternalId = movieActorResult.ExternalId,
                Name = movieActorResult.Name
            };
        }
    }
}