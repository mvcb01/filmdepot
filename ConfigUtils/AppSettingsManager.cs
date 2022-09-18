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
            private int _numberOfExecutions;

            public int NumberOfExecutions
            {
                get => _numberOfExecutions;
                set
                {
                    _numberOfExecutions = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "number of executions should be > 0");
                }
            }

            private TimeSpan _perTimeSpan;

            public TimeSpan PerTimeSpan
            {
                get => _perTimeSpan;
                set 
                {
                    _perTimeSpan = value.TotalMilliseconds > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "should be a positive timespan");
                }
            }

            private int? _maxBurst;

            public int? MaxBurst
            {
                get => _maxBurst;
                set
                {
                    _maxBurst = (value == null || value > 0) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "max burst should be null or > 0");
                }
            }

        }

        class RetryPolicyConfig : IRetryPolicyConfig
        {
            private int _retryCount;

            public int RetryCount
            {
                get => _retryCount;
                set
                {
                    _retryCount = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "retry count should be > 0");
                }
            }

            private TimeSpan _sleepDuration;

            public TimeSpan SleepDuration
            {
                get => _sleepDuration;
                set
                {
                    _sleepDuration = value.TotalMilliseconds > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "should be a positive timespan");
                }
            }
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
