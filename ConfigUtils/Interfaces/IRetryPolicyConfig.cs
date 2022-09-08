using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigUtils.Interfaces
{
    public interface IRetryPolicyConfig
    {
        int RetryCount { get; set; }

        TimeSpan SleepDuration { get; set; }
    }
}
