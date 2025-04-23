using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

var services = new ServiceCollection();
var policyKey = "TodoClient";

services.AddPolicyRegistry((sp, reg) =>
{
    reg.Add(policyKey, BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy());
});

services.AddHttpClient<ITodoClient, TodoClient>().ConfigureHttpClient(client =>
{
    client.BaseAddress = new Uri("https://dummyjson.com");
    // client.BaseAddress = new Uri("http://localhost:2000");
}).AddPolicyHandlerFromRegistry(policyKey);

var serviceProvider = services.BuildServiceProvider();
var todoClient = serviceProvider.GetRequiredService<ITodoClient>();

var result = await todoClient.GetTodoItem();

Console.WriteLine(result);


static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerRetryAndTimeoutPerRetryPolicy()
{
    var retry = HttpPolicyExtensions
        .HandleTransientHttpError()
        .RetryAsync(1);

    var requestTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(50));

    return Policy.WrapAsync(retry, requestTimeout);
}
