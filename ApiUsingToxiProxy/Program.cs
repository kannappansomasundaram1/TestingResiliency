using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPolicyRegistry((sp, reg) =>
{
    reg.Add("todoclientPolicy", BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy());
});

builder.AddServiceDefaults();
builder.Services.AddHttpClient<ITodoClient, TodoClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://localhost:8444");
        client.DefaultRequestHeaders.Add("Host", "dummyjson.com");
    })
    .AddResilienceHandler("MyResiliencePolicy", (p, c) => GetResilienceBuilder(p));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/todo/{id}", (string id, ITodoClient todoClient) => todoClient.GetTodo(id))
    .WithName("TodoItem");

app.Run();


IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy()
{
    var retry = HttpPolicyExtensions
        .HandleTransientHttpError()
        .RetryAsync(1);

    var requestTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(50));

    return Policy.WrapAsync(retry, requestTimeout);
}

static ResiliencePipeline<HttpResponseMessage> GetResilienceBuilder(
    ResiliencePipelineBuilder<HttpResponseMessage> resiliencePipelineBuilder)
{
    return resiliencePipelineBuilder
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromMinutes(1), MinimumThroughput = 5, FailureRatio = 0.7, BreakDuration = TimeSpan.FromSeconds(5)
        })
        // .AddTimeout(TimeSpan.FromSeconds(1))
        .Build();
}