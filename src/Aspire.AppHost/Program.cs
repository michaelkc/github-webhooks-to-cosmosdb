var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
var cosmos = builder.AddAzureCosmosDB("cosmos-db-resource")
    .RunAsPreviewEmulator(o => o
    .WithGatewayPort(8081)
    .WithLifetime(ContainerLifetime.Persistent)
        .WithDataExplorer(62631));
#pragma warning restore ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var database = cosmos.AddCosmosDatabase("githubevents");
database.AddContainer("githubeventscontainer", "/id");

var func = builder
    .AddAzureFunctionsProject<Projects.GitHubWebhookFunction>("githubwebhookfunction")
    .WithReference(database)
    .WaitFor(database);
builder.Build().Run();
