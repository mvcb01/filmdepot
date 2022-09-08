using System;

namespace ConfigUtils.Interfaces
{
    public interface IRetryPolicyConfig
    {
        int RetryCount { get; set; }

        TimeSpan SleepDuration { get; set; }
    }
}
