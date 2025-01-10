using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PostHog;
using System.Security.Claims;
using PostHog.Config;

namespace HogTied.Web;

public class PostHogPageViewFilter(IOptions<PostHogOptions> options, IPostHogClient postHogClient) : IAsyncPageFilter
{
    readonly PostHogOptions _options = options.Value;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (_options.ProjectApiKey is not null)
        {
            var user = context.HttpContext.User;
            var distinctId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (distinctId is not null)
            {
                postHogClient.Capture(
                    distinctId,
                    eventName: "page_view",
                    properties: new()
                    {
                        ["$current_url"] = context.HttpContext.Request.Path.Value ?? "Unknown"
                    });
            }
        }

        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
}