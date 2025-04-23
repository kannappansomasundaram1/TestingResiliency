using System.Net.Http.Json;

public class TodoClient(HttpClient httpClient) : ITodoClient
{
    public async Task<TodoItem?> GetTodoItem()
    {
        return await httpClient.GetFromJsonAsync<TodoItem>("todos/1");
    }
}
