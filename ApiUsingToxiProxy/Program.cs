using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();


builder.AddServiceDefaults();
builder.Services.AddHttpClient<ITodoClient, TodoClient>()
    .ConfigureHttpClient(client =>
    {
        // client.BaseAddress = new Uri("https://localhost:8443");
        // client.DefaultRequestHeaders.Add("Host", "dummyjson.com");
        client.BaseAddress = new Uri("https://dummyjson.com");
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


static void GetResilienceBuilder(ResiliencePipelineBuilder<HttpResponseMessage> resiliencePipelineBuilder)
{
    resiliencePipelineBuilder
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromSeconds(1)
        })
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromMinutes(1), 
            MinimumThroughput = 5, 
            FailureRatio = 0.7,
            BreakDuration = TimeSpan.FromSeconds(10)
        })
        .AddTimeout(TimeSpan.FromSeconds(1))
        .Build();
}