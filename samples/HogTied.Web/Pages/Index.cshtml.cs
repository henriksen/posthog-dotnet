using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PostHog;
using PostHog.FeatureFlags;

namespace HogTied.Web.Pages;

public class IndexModel(
    IOptions<PostHogOptions> options) : PageModel
{
    // This is a hardcoded distinct user ID for the purposes of this demo.
    // We would normally use a user ID from our application's authentication system.
    private const string DistinctUserId = "12345";

    public bool ApiKeyIsSet { get; private set; }

    public bool BonanzaEnabled { get; private set; }

    public string? IdentifyResult { get; private set; }

    public async Task OnGetAsync()
    {
        ApiKeyIsSet = options.Value.ProjectApiKey is not (null or []);

        if (ApiKeyIsSet)
        {
            // Identify the current user.
            using var postHogClient = new PostHogClient(options.Value.ProjectApiKey!);
            IdentifyResult = await postHogClient.IdentifyAsync(DistinctUserId);
            var features = new FeatureFlagsClient(options.Value.ProjectApiKey!);
            var flags = await features.GetFeatureFlagsAsync(DistinctUserId);
            BonanzaEnabled = flags.IsFeatureEnabled("hogtied-homepage-bonanza");
        }
    }
}
