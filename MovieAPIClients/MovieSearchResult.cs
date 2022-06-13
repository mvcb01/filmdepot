using FilmDomain.Entities;

namespace MovieAPIClients
{
    public class MovieSearchResult
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