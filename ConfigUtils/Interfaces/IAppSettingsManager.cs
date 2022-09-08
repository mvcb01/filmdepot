using System.Collections.Generic;

namespace ConfigUtils.Interfaces
{
    public interface IAppSettingsManager
    {
        string GetConnectionString(string dbName);

        string GetMovieWarehouseDirectory();

        string GetWarehouseContentsTextFilesDirectory();

        IEnumerable<string> GetFilesToIgnore();

        Dictionary<string, Dictionary<string, string>> GetManualMovieRips();

        string GetApiKey(string keyName);

        IEnumerable<string> GetRipFilenamesToIgnoreOnLinking();

        Dictionary<string, int> GetManualExternalIds();

        IRateLimitPolicyConfig GetRateLimitPolicyConfig();

        //IRetryPolicyConfig GetRetryPolicyConfig();

    }
}