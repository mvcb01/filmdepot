using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using ConfigUtils.Interfaces;

namespace ConfigUtils
{
    public class AppSettingsManager : IAppSettingsManager
    {
        public IConfigurationRoot ConfigRoot { get; init; }

        public AppSettingsManager(string filename)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(filename).AddUserSecrets<AppSettingsManager>();
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
                throw new Exception($"Api key desconhecida: {keyName}");
            }
            return result;
        }
    }
}
