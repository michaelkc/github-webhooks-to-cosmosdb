using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitHubWebhookFunction;


public class GitHubEvent
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("eventBody")]
    public JsonElement? EventBody { get; set; }

    [JsonPropertyName("partitionKey")]
    public string PartitionKey
    {
        get
        {
            var id =
                EventBody?
                 .GetProperty("repository")
                 .GetProperty("id")
                 .GetInt64();
            return id.HasValue 
                ? "repo_" + id.ToString() 
                : "no_repo_partition";
        }
    }
}