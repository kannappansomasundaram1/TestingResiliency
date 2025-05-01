using System.Net.Http.Json;

public class TodoClient(HttpClient httpClient) : ITodoClient
{
    public async Task<TodoItem?> GetTodo(string itemId)
    {
        return await httpClient.GetFromJsonAsync<TodoItem?>($"todos/{itemId}");
    }
}

public interface ITodoClient
{
    Task<TodoItem?> GetTodo(string itemId);
}


