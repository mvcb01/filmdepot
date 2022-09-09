using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using ConfigUtils.Interfaces;
using System.Net.WebSockets;

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
            configBuilder.AddJsonFile("appsettings.json");

            var env = Environment.GetEnvironmentVariable("FILMCRUD_ENVIRONMENT");
            if (env != null)
            {
                configBuilder.AddJsonFile($"appsettings.{env}.json");
            }

            configBuilder.AddUserSecrets<AppSettingsManager>();

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

        public IEnumerable<string> GetRipFilenamesToIgnoreOnLinking()
        {
            return ConfigRoot.GetSection("ManualRipToMovieLinks").GetSection("RipFilenamesToIgnore").Get<IEnumerable<string>>();
        }

        public Dictionary<string, int> GetManualExternalIds()
        {
            return ConfigRoot.GetSection("ManualRipToMovieLinks").GetSection("ManualExternalIds").Get<Dictionary<string, int>>();
        }

        // ------------------------
        // Policy methods

        // no need to publicly expose these classes
        class RateLimitPolicyConfig : IRateLimitPolicyConfig
        {
            private int numberOfExecutions;

            public int NumberOfExecutions
            {
                get { return numberOfExecutions; }
                set
                {
                    if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), "should be > 0");
                    numberOfExecutions = value;
                }
            }

            private TimeSpan perTimeSpan;

            public TimeSpan PerTimeSpan
            {
                get { return perTimeSpan; }
                set 
                {
                    if (value.Milliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(value), "should be a positive timespan");
                    perTimeSpan = value; 
                }
            }

            private int? maxBurst;

            public int? MaxBurst
            {
                get { return maxBurst; }
                set
                {
                    if (value != null && value < 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "should be null or > 0");
                    }
                    maxBurst = value; 
                }
            }

        }

        class RetryPolicyConfig : IRetryPolicyConfig
        {
            public int RetryCount { get; set; }
            public TimeSpan SleepDuration { get; set; }
        }

        public IRateLimitPolicyConfig GetRateLimitPolicyConfig()
        {
            var cfgDict = ConfigRoot.GetSection("Policies").GetSection("RateLimit").Get<Dictionary<string, int>>();
            int cfgMaxBurst = cfgDict.GetValueOrDefault("MaxBurst", -1);
            return new RateLimitPolicyConfig()
            {
                NumberOfExecutions = cfgDict["NumberOfExecutions"],
                PerTimeSpan = TimeSpan.FromMilliseconds(cfgDict["PerTimeSpanMilliseconds"]),
                MaxBurst = cfgMaxBurst == -1 ? null : cfgMaxBurst
            };
        }

        public IRetryPolicyConfig GetRetryPolicyConfig()
        {
            var cfgDict = ConfigRoot.GetSection("Policies").GetSection("Retry").Get<Dictionary<string, int>>();
            return new RetryPolicyConfig() 
            {
                RetryCount = cfgDict["RetryCount"],
                SleepDuration = TimeSpan.FromMilliseconds(cfgDict["SleepDurationMilliseconds"])
            };
        }
    }
}
