using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Extensions.Http;

var services = new ServiceCollection();
var policyKey = "DummyJsonClient";

services.AddPolicyRegistry((sp, reg) => { reg.Add(policyKey, BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy()); });

services.AddHttpClient<ITodoClient, TodoClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://localhost:8443");
        client.DefaultRequestHeaders.Add("Host", "dummyjson.com");
    })
    .AddResilienceHandler("MyResiliencePolicy", (p,c) => GetResilienceBuilder(p));

var serviceProvider = services.BuildServiceProvider();
var rdaClient = serviceProvider.GetRequiredService<ITodoClient>();

var result = await rdaClient.GetTodo("1");

Console.WriteLine(result);


static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy()
{
    var retry = HttpPolicyExtensions
        .HandleTransientHttpError()
        .RetryAsync(1);

    var requestTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(50));

    return Policy.WrapAsync(retry, requestTimeout);
}
static ResiliencePipeline<HttpResponseMessage> GetResilienceBuilder(ResiliencePipelineBuilder<HttpResponseMessage> resiliencePipelineBuilder)
{
    return resiliencePipelineBuilder
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromMinutes(1), MinimumThroughput = 5, FailureRatio = 0.7
        })
        // .AddTimeout(TimeSpan.FromSeconds(1))
        .Build();
}