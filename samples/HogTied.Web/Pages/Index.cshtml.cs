using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PostHog;
using PostHog.FeatureFlags;

namespace HogTied.Web.Pages;

public class IndexModel(IOptions<PostHogOptions> options, IPostHogClient postHogClient) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public string? UserId { get; private set; }

    [BindProperty]
    public string? FakeUserId { get; set; } = "12345";

    public bool ApiKeyIsSet { get; private set; }

    public bool BonanzaEnabled { get; private set; }

    public bool HomepageUser { get; private set; }

    [Required]
    public string? EventName { get; set; } = "plan_purchased";

    public PostHogOptions PostHogOptions => options.Value;

    public async Task OnGetAsync()
    {
        ApiKeyIsSet = options.Value.ProjectApiKey is not (null or []);

        // Check if the user is authenticated and get their user id.
        UserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : FakeUserId;

        if (ApiKeyIsSet && UserId is not null)
        {
            // Identify the current user.
            var features = new FeatureFlagsClient(options.Value.ProjectApiKey!);
            var flags = await features.GetFeatureFlagsAsync(UserId, HttpContext.RequestAborted);
            BonanzaEnabled = flags.IsFeatureEnabled("hogtied-homepage-bonanza");
            HomepageUser = flags.IsFeatureEnabled("hogtied-homepage-user");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await OnGetAsync();
        if (!ApiKeyIsSet || UserId is null)
        {
            return RedirectToPage();
        }

        // Send a custom purchased plan event
        postHogClient.Capture(
            UserId,
            eventName: EventName ?? "plan_purchased",
            properties: new()
            {
                ["plan"] = "free",
                ["price"] = "$29.99"
            });

        StatusMessage = "Event captured! Events are sent asynchronously, so it may take a few seconds to appear in PostHog.";

        return RedirectToPage();
    }
}
