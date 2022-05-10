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

            AppSettingsManager appSettingsManager = new("appsettings.json");
            string apiKey = appSettingsManager.GetApiKey("TheMovieDb");
            var apiCaller = new TheMovieDbAPIClient(apiKey);
            {}

            var result = await apiCaller.SearchMovieAsync("the tenant");
            var resultList = result.ToList();
            {}
        }
    }
}
