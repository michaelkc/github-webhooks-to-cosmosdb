using GitHubWebhookFunction;
using GitHubWebhookFunction.Infra;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octokit.Webhooks;

var builder = FunctionsApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

builder.Services
    
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.Services.AddScoped<FunctionItems>();
builder.Services.AddScoped<WebhookEventProcessor, PassthroughWebhookEventProcessor>();

//builder.Services.AddSingleton(s =>
//{
//    var cosmosClient = new CosmosClient(builder.Configuration["githubevents"], new CosmosClientOptions
//    {
//        ConnectionMode = ConnectionMode.Gateway,
//    });
//    return cosmosClient;


//});



builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

builder.Build().Run();
