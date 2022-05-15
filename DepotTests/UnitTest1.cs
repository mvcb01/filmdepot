using System;
using Xunit;
using MovieAPIClients.TheMovieDb;
using ConfigUtils;
using System.Linq;

namespace DepotTests
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {

            var appSettingsManager = new AppSettingsManager();
            string apiKey = appSettingsManager.GetApiKey("TheMovieDb");
            var apiCaller = new TheMovieDbAPIClient(apiKey);
            {}

            // var result = await apiCaller.SearchMovieAsync("the tenant");
            // var result = await apiCaller.GetMovieGenresAsync(11482);
            var result = await apiCaller.GetMovieActorsAsync(11482);

            var resultList = result.ToList();
            {}
        }
    }
}
