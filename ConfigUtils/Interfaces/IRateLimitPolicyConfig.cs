using System;

namespace ConfigUtils.Interfaces
{
    public interface IRateLimitPolicyConfig
    {
        int NumberOfExecutions { get; set; }

        TimeSpan PerTimeSpan { get; set; }

        int? MaxBurst { get; set; }
    }
}
