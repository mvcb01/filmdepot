using System;
using Xunit;
using MovieAPIClients.TheMovieDb;
using ConfigUtils;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection.Emit;
using Polly;
using Polly.Retry;
using Polly.RateLimit;
using Polly.Wrap;

namespace DepotTests
{
    public class RetryWithLimitTests
    {
        public int MethodCallCount = 0;

        public async Task<string> MyRateLimitedMethodAsync()
        {
            await Task.Delay(50);

            this.MethodCallCount += 1;

            return DateTime.Now.ToString("T");
        }

        public async Task<IEnumerable<string>> MethodWithRetriesAsync()
        {
            AsyncRateLimitPolicy limitPolicy = Policy.RateLimitAsync(
                1,
                TimeSpan.FromSeconds(1));

            AsyncRetryPolicy retryPolicy = Policy
                .Handle<RateLimitRejectedException>()
                .WaitAndRetryAsync(new TimeSpan[] {
                    TimeSpan.FromMilliseconds(20),
                    TimeSpan.FromMilliseconds(40),
                    TimeSpan.FromMilliseconds(25) });

            AsyncPolicyWrap policyWrap = Policy.WrapAsync(retryPolicy, limitPolicy);

            var resultList = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var resultThis = await policyWrap.ExecuteAsync(() => MyRateLimitedMethodAsync());
                    resultList.Add(resultThis);
                }
                catch (RateLimitRejectedException)
                {

                    resultList.Add($"iter: {i}, dt: {DateTime.Now}");
                }
                
            }

            return resultList;
        }
    }


    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            //var dummyClient = new TheMovieDbAPIClientRateLimited("dummyKey");
            //string x = this.GetType().FullName;

            //await dummyClient.DummyExecutionsAsync();

            //{ }

            await Task.Delay(101);

        }


        [Fact]
        public async Task MethodWithRetriesAsyncTests()
        {
            var testObj = new RetryWithLimitTests();
            var result = await testObj.MethodWithRetriesAsync();
            
            var callCount = testObj.MethodCallCount;
            { }
        }


    }
}
