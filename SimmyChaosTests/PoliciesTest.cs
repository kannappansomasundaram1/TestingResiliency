using System.Net;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace SimmyChaosTests;

public class PoliciesTest
{
    [Fact]
    public async Task TimeoutTest()
    {
        var monkeyPolicy = MonkeyPolicy.InjectLatencyAsync<HttpResponseMessage>(o =>
            o.Latency(TimeSpan.FromSeconds(2)).Enabled().InjectionRate(1));
        var policy = Policy.WrapAsync(BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy(), monkeyPolicy);

        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage())));
    }

    [Fact]
    public async Task BrokenCircuitTest()
    {
        var monkeyPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(c =>
            c.Result((_, __) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)))
                .InjectionRate(1).Enabled());
        var policy = Policy.WrapAsync(BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy(), monkeyPolicy);

        for (var i = 0; i < 5; i++)
        {
            await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage()));
        }

        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage())));
    }


    static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy()
    {
        var retry = HttpPolicyExtensions
            .HandleTransientHttpError()
            .AdvancedCircuitBreakerAsync(0.7, TimeSpan.FromSeconds(1), 5, TimeSpan.FromSeconds(2));
        var requestTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1));

        return Policy.WrapAsync(retry, requestTimeout);
    }
}