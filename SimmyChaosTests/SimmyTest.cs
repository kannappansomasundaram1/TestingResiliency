using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Simmy;
using Polly.Timeout;

namespace SimmyChaosTests;

public class SimmyTest
{
    //https://www.pollydocs.org/chaos/#usage

    [Fact]
    public async Task TimeoutTest()
    {
        var policy = GetResilienceBuilder()
            .AddChaosLatency(1.0, TimeSpan.FromSeconds(5))
            .Build();

        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await policy.ExecuteAsync(_ => ValueTask.CompletedTask));
    }

    [Fact]
    public async Task BrokenCircuitTest()
    {
        var policy = GetResilienceBuilder()
            .AddChaosFault(1.0, () => throw new Exception())
            .Build();

        for (var i = 0; i < 5; i++)
        {
            try
            {
                await policy.ExecuteAsync(_ => ValueTask.CompletedTask);
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await policy.ExecuteAsync(_ => ValueTask.CompletedTask));
    }

    public static ResiliencePipelineBuilder GetResilienceBuilder()
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromMinutes(1), MinimumThroughput = 5, FailureRatio = 0.7
            })
            .AddTimeout(TimeSpan.FromSeconds(1));
    }
    
    
    //TODO Use this in tests
    static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy()
    {
        var retry = HttpPolicyExtensions
            .HandleTransientHttpError()
            .RetryAsync(1);

        var requestTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(50));

        return Policy.WrapAsync(retry, requestTimeout);
    }
}
