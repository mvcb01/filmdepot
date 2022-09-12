using System;
using Polly;
using Polly.RateLimit;
using Polly.Retry;

using ConfigUtils.Interfaces;

namespace FilmCRUD.Helpers
{
    public static class APIClientPolicyBuilder
    {

        public static AsyncRateLimitPolicy GetAsyncRateLimitPolicy(IRateLimitPolicyConfig rateLimitConfig)
        {
            if (rateLimitConfig.MaxBurst == null)
            {
                return Policy.RateLimitAsync(rateLimitConfig.NumberOfExecutions, rateLimitConfig.PerTimeSpan);
            }
            return Policy.RateLimitAsync(
                rateLimitConfig.NumberOfExecutions,
                rateLimitConfig.PerTimeSpan,
                (int)rateLimitConfig.MaxBurst
            );
        }


        public static AsyncRetryPolicy GetAsyncRetryPolicy<TException>(IRetryPolicyConfig retryConfig) where TException : Exception
        {
            return Policy.Handle<TException>().WaitAndRetryAsync(retryConfig.RetryCount, _ => retryConfig.SleepDuration);
        }
    }
}
