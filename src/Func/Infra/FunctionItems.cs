namespace GitHubWebhookFunction.Infra;

// Like HttpContext.Items, but for Functions
public class FunctionItems
{
    private readonly Dictionary<string, object> _items = new();

    public void Set<T>(string key, T value) => _items[key] = value!;
    public T? Get<T>(string key) => _items.TryGetValue(key, out var value) ? (T)value : default;
}