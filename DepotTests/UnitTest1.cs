using System;
using Xunit;
using MovieAPIClients.TheMovieDb;
using ConfigUtils;
using System.Linq;
using System.Threading.Tasks;
using MovieAPIClients;
using System.Collections.Generic;

namespace DepotTests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            string apiKey = "a8eaaf114c647102f1cf89f260985ce5";

            var apiClient = new TheMovieDbAPIClient(apiKey);

            string title = "alps";
            int releaseDate = 2011;

            IEnumerable<MovieSearchResult> allResults = await apiClient.SearchMovieAsync("alps");

            IEnumerable<MovieSearchResult> withDate = allResults.Where(r => r.ReleaseDate == releaseDate);

            { }
        }
    }
}
