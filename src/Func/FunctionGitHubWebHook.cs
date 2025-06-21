using GitHubWebhookFunction.Infra;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit.Webhooks;
using Octokit.Webhooks.AzureFunctions;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GitHubWebhookFunction;

public class FunctionGitHubWebHook
{
    private readonly ILogger<FunctionGitHubWebHook> logger;
    //private readonly MyWebhookEventProcessor _eventProcessor;

    //private readonly CosmosClient _cosmosClient;
    //private readonly Container _container;
    //public GitHubWebhookHandler(CosmosClient cosmosClient, ILogger<GitHubWebhookHandler> logger)
    //{
    //    _cosmosClient = cosmosClient;
    //    this.logger = logger;
    //    _container = _cosmosClient.GetDatabase("githubevents").GetContainer("githubeventscontainer");
    //}

    public FunctionGitHubWebHook(ILogger<FunctionGitHubWebHook> logger)
    {
        // , MyWebhookEventProcessor eventProcessor
        this.logger = logger;
        //_eventProcessor = eventProcessor;
    }

    [Function("GitHubWebhook")]
    public async Task<MultiResponse> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("HttpExample");
        logger.LogInformation("GitHub webhook received");
        //var stream = req.Body;
        //var httpReqData = await executionContext.GetHttpRequestDataAsync();
        ////HttpRequestStream
        //string body = null;
        //if (httpReqData != null)
        //{
        //    using var reader = new StreamReader(httpReqData.Body, Encoding.UTF8, false, 1024, leaveOpen: true);
        //    var payload = await reader.ReadToEndAsync();
        //    body = payload;

        //    httpReqData.Body.Seek(0L, SeekOrigin.Begin);
        //}



        // This verifies that the request is a valid GitHub webhook request,
        // including checking the signature and content type.
        // If it returns ok, we can proceed to run our output binding to CosmosDB.
        //var innerFunc = executionContext.InstanceServices.GetRequiredService<PassthroughWebhookEventProcessor>();


        var opt = Options.Create(new GitHubWebhooksOptions
        {
            Secret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET")
        });

        var innerFunc = new GitHubWebhooksHttpFunction(opt);
        var innerFuncResult = await innerFunc.MapGitHubWebhooksAsync(req, executionContext);

        if (innerFuncResult!.StatusCode != HttpStatusCode.OK)
        {
            return new MultiResponse
            {
                Response = innerFuncResult
            };
        }
        var functionItems = executionContext.InstanceServices.GetRequiredService<FunctionItems>();

        var body = functionItems.Get<string>(Constants.ItemKeyBody);
        if (body == null)
        {
            logger.LogError("Null body. Cannot process deployment event.");
            return new MultiResponse
            {
                Response = req.CreateResponse(HttpStatusCode.InternalServerError)
            };
        }
        var rawJson = JsonSerializer.Deserialize<JsonElement>(body);


        var response = req.CreateResponse(HttpStatusCode.NoContent);

        var githubEvent = new GitHubEvent
        {
            Id = Guid.NewGuid().ToString(),
            EventBody = rawJson
        };


        return new MultiResponse()
        {
            GitHubEvent = githubEvent,
            Response = response
        };
    }

    private bool ValidateSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("No signature provided");
            return false;
        }

        // Get the webhook secret from environment variables
        var secret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET");
        if (string.IsNullOrEmpty(secret))
        {
            logger.LogError("GITHUB_WEBHOOK_SECRET environment variable not set");
            return false;
        }

        // GitHub sends signature as "sha256=<hash>"
        if (!signature.StartsWith("sha256="))
        {
            logger.LogWarning("Invalid signature format");
            return false;
        }

        var expectedHash = signature.Substring(7); // Remove "sha256=" prefix
        
        // Compute HMAC-SHA256 hash
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedHashString = Convert.ToHexString(computedHash).ToLower();
            
            // Use secure comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash.ToLower()),
                Encoding.UTF8.GetBytes(computedHashString)
            );
        }
    }
}

//var cosmos = builder.AddAzureCosmosDB("cosmos-db-resource")
//    .RunAsEmulator();
//    var database = cosmos.AddCosmosDatabase("githubevents");
//database.AddContainer("githubeventscontainer", "/id");



public class MultiResponse
{
    // Output binding is using http it is difficult and error prone to get
    // Aspire to use https and get the cert trusted everywhere.
    // So we use explicit writing to CosmosDB instead.
    [CosmosDBOutput(
        databaseName: "githubevents",
        containerName: "githubeventscontainer",
        Connection = "githubevents",
        PartitionKey = "/id")]
    public GitHubEvent? GitHubEvent { get; set; }

    [HttpResult]
    public required HttpResponseData Response { get; set; }
}

