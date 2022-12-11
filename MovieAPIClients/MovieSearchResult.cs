using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace MovieAPIClients
{
    public class MovieSearchResult : IEntityWithTitleAndOriginalTitle
    {
        public int ExternalId { get; set; }

        public string Title { get; set; }

        public string OriginalTitle { get; set; }

        public int ReleaseDate { get; set; }

        public static explicit operator Movie(MovieSearchResult movieSearchResult)
        {
            return new Movie() {
                ExternalId = movieSearchResult.ExternalId,
                Title = movieSearchResult.Title,
                OriginalTitle = movieSearchResult.OriginalTitle,
                ReleaseDate = movieSearchResult.ReleaseDate
                };
        }
    }
}