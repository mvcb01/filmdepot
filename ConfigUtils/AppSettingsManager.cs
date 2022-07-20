using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using ConfigUtils.Interfaces;

namespace ConfigUtils
{
    public class AppSettingsManager : IAppSettingsManager
    {
        public IConfigurationRoot ConfigRoot { get; init; }

        public AppSettingsManager()
        {
            // general access order:
            //    user secrets
            //    appsettings.ENV.json
            //    appsettings.json
            // NOTA: access order for different ENVs:
            //      https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#hi2low
            var configBuilder = new ConfigurationBuilder();
            configBuilder
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<AppSettingsManager>();

            var env = Environment.GetEnvironmentVariable("FILMCRUD_ENVIRONMENT");
            if (env != null)
            {
                configBuilder.AddJsonFile($"appsettings.{env}.json");
            }

            this.ConfigRoot = configBuilder.Build();
        }

        public string GetConnectionString(string dbName)
        {
            return this.ConfigRoot.GetConnectionString(dbName);
        }

        public string GetMovieWarehouseDirectory()
        {
            return ConfigRoot.GetSection("MovieWarehouse").Value;
        }

        public string GetWarehouseContentsTextFilesDirectory()
        {
            return ConfigRoot.GetSection("WarehouseContentsTextFilesDirectory").Value;
        }

        public IEnumerable<string> GetFilesToIgnore()
        {
            return ConfigRoot.GetSection("FilesToIgnore").Get<IEnumerable<string>>();
        }

        public Dictionary<string, Dictionary<string, string>> GetManualMovieRips()
        {
            return ConfigRoot.GetSection("ManualMovieRips").Get<Dictionary<string, Dictionary<string, string>>>();
        }

        public string GetApiKey(string keyName)
        {
            string keyToGet = $"ApiKeys:{keyName}";
            var result = this.ConfigRoot[keyToGet];
            if (result == null)
            {
                throw new Exception($"Unknown Api key: {keyName}");
            }
            return result;
        }

        public IEnumerable<string> GetRipFilenamesToIgnoreOnLinking()
        {
            return ConfigRoot.GetSection("ManualRipToMovieLinks").GetSection("RipFilenamesToIgnore").Get<IEnumerable<string>>();
        }

        public Dictionary<string, int> GetManualExternalIds()
        {
            return ConfigRoot.GetSection("ManualRipToMovieLinks").GetSection("ManualExternalIds").Get<Dictionary<string, int>>();
        }
    }
}
