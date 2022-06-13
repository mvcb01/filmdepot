using MovieAPIClients.TheMovieDb;

namespace MovieAPIClients.Extensions
{
    public static class SearchResultExtensions
    {
        public static MovieSearchResult ToMovieSearchResult(this MovieSearchResultTMDB movieSearchResultTMDB)
        {
            return new MovieSearchResult() {
                ExternalId = movieSearchResultTMDB.ExternalId,
                Title = movieSearchResultTMDB.Title,
                OriginalTitle = movieSearchResultTMDB.OriginalTitle,
                ReleaseDate = movieSearchResultTMDB.ReleaseDate
                };
        }
    }
}