using System;
using Xunit;
using MovieAPIClients.TheMovieDb;
using ConfigUtils;

namespace DepotTests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {

            AppSettingsManager appSettingsManager = new("appsettings.json");

        }
    }
}
