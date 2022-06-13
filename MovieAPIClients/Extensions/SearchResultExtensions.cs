using MovieAPIClients.TheMovieDb;

namespace MovieAPIClients.Extensions
{
    public static class SearchResultExtensions
    {
        public static MovieGenreResult ToMovieGenreResult(this MovieGenreResultTMDB movieGenreResultTMDB)
        {
            return new MovieGenreResult() {
                ExternalId = movieGenreResultTMDB.ExternalId,
                Name = movieGenreResultTMDB.Name
                };
        }
    }
}