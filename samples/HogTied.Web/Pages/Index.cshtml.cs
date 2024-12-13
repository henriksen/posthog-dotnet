using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PostHog;
using PostHog.FeatureFlags;

namespace HogTied.Web.Pages;

public class IndexModel(IOptions<PostHogOptions> options) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public string? UserId { get; private set; }

    public bool ApiKeyIsSet { get; private set; }

    public bool BonanzaEnabled { get; private set; }

    public bool HomepageUser { get; private set; }

    public async Task OnGetAsync()
    {
        ApiKeyIsSet = options.Value.ProjectApiKey is not (null or []);

        // Check if the user is authenticated and get their user id.
        UserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        if (ApiKeyIsSet && UserId is not null)
        {
            // Identify the current user.
            using var postHogClient = new PostHogClient(options.Value.ProjectApiKey!);
            var features = new FeatureFlagsClient(options.Value.ProjectApiKey!);
            var flags = await features.GetFeatureFlagsAsync(UserId, HttpContext.RequestAborted);
            BonanzaEnabled = flags.IsFeatureEnabled("hogtied-homepage-bonanza");
            HomepageUser = flags.IsFeatureEnabled("hogtied-homepage-user");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await OnGetAsync();
        if (ApiKeyIsSet && UserId is not null)
        {
            // Send a custom purchased plan event
            using var postHogClient = new PostHogClient(options.Value.ProjectApiKey!);
            var result = await postHogClient.CaptureAsync(
                UserId,
                eventName: "purchased_plan",
                properties: new()
                {
                    ["plan"] = "free",
                    ["price"] = "$29.99"
                },
                cancellationToken: HttpContext.RequestAborted);
            StatusMessage = "Plan purchased! "
                + (result.Status is 1
                    ? "Event sent successfully."
                    : "Failed to send event.");
        }

        return RedirectToPage();
    }
}
