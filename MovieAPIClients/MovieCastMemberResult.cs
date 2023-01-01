using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace MovieAPIClients
{
    public class MovieCastMemberResult : IExternalEntity
    {
        public int ExternalId { get; set; }

        public string Name { get; set; }

        public static explicit operator CastMember(MovieCastMemberResult movieCastMemberResult)
        {
            return new CastMember() {
                ExternalId = movieCastMemberResult.ExternalId,
                Name = movieCastMemberResult.Name
            };
        }
    }
}