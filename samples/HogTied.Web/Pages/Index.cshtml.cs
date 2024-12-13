using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PostHog;
using PostHog.FeatureFlags;

namespace HogTied.Web.Pages;

public class IndexModel(IOptions<PostHogOptions> options) : PageModel
{
    public string? UserId { get; private set; }

    public bool ApiKeyIsSet { get; private set; }

    public bool BonanzaEnabled { get; private set; }

    public string? IdentifyResult { get; private set; }

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
            IdentifyResult = await postHogClient.IdentifyAsync(UserId);
            var features = new FeatureFlagsClient(options.Value.ProjectApiKey!);
            var flags = await features.GetFeatureFlagsAsync(UserId);
            BonanzaEnabled = flags.IsFeatureEnabled("hogtied-homepage-bonanza");
        }
    }
}
