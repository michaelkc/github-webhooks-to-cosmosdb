using GitHubWebhookFunction.Infra;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Octokit.Webhooks;

namespace GitHubWebhookFunction;

public sealed class PassthroughWebhookEventProcessor(
    FunctionItems functionItems, 
    ILogger<WebhookEventProcessor> logger) : WebhookEventProcessor
{
    public override async Task ProcessWebhookAsync(IDictionary<string, StringValues> headers, string body)
    {
        SaveRaw(body);
        await base.ProcessWebhookAsync(headers, body);
    }

    private void SaveRaw(string body)
    {
        functionItems.Set(Constants.ItemKeyBody, body);
    }

    //protected override Task ProcessDeploymentWebhookAsync(WebhookHeaders headers, DeploymentEvent deploymentEvent, DeploymentAction action)
    //{
    //    var httpContext = httpContextAccessor?.HttpContext;
    //    if (httpContext == null)
    //    {
    //        logger.LogError("HttpContext is null. Cannot process deployment event.");
    //        return Task.CompletedTask;
    //    }
    //    httpContext.Items["webhook_result"] = deploymentEvent;
    //    return Task.CompletedTask;
    //}
}

