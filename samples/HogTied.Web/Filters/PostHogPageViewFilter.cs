using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PostHog;
using System.Security.Claims;

namespace HogTied.Web;

public class PostHogPageViewFilter(IOptions<PostHogOptions> options) : IAsyncPageFilter
{
    private readonly PostHogOptions _options = options.Value;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (_options.ProjectApiKey is not null)
        {
            var user = context.HttpContext.User;
            var distinctId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (distinctId is not null)
            {
                using var postHogClient = new PostHogClient(_options.ProjectApiKey);
                await postHogClient.CaptureAsync(
                    distinctId,
                    eventName: "page_view",
                    properties: new()
                    {
                        ["path"] = context.HttpContext.Request.Path.Value ?? "Unknown"
                    });
            }
        }

        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
}