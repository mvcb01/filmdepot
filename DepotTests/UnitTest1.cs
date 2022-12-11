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
            var client = new TheMovieDbAPIClient("a8eaaf114c647102f1cf89f260985ce5");

            var results = await client.SearchMovieAsync("dead mans shoes");
        }
    }
}
