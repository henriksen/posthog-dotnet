using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PostHog;
using PostHog.Config;
using PostHog.Features;

namespace HogTied.Web.Pages;

public class IndexModel(IOptions<PostHogOptions> options, IPostHogClient postHogClient) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public string? UserId { get; private set; }

    [BindProperty]
    public string? FakeUserId { get; set; } = "12345";

    public bool ApiKeyIsSet { get; private set; }

    public bool? NonExistentFlag { get; private set; }

    public Dictionary<string, (FeatureFlag, bool?)> FeatureFlags { get; private set; } = new();

    [BindProperty]
    [Required]
    public string? EventName { get; set; } = "plan_purchased";

    [BindProperty]
    public GroupModel Group { get; set; } = new();

    public PostHogOptions PostHogOptions => options.Value;

    [BindProperty(SupportsGet = true)]
    [FromQuery]
    public string? ProjectSize { get; set; }

    public async Task OnGetAsync()
    {
        ApiKeyIsSet = options.Value.ProjectApiKey is not (null or []);

        // Check if the user is authenticated and get their user id.
        UserId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : FakeUserId;

        if (ApiKeyIsSet && UserId is not null)
        {
            // Identify the current user if they're authenticated.
            if (User.Identity?.IsAuthenticated == true)
            {
                await postHogClient.IdentifyPersonAsync(
                    UserId,
                    email: User.FindFirst(ClaimTypes.Email)?.Value,
                    name: User.FindFirst(ClaimTypes.Name)?.Value,
                    HttpContext.RequestAborted);
            }

            var flags = await postHogClient.GetFeatureFlagsAsync(
                UserId,
                personProperties: null,
                groupProperties:
                [
                    new Group("organization","01943db3-83be-0000-e7ea-ecae4d9b5afb"),
                    new Group("project", "aaaa-bbbb-cccc", new Dictionary<string, object>
                    {
                        ["size"] = ProjectSize ?? "large"
                    })
                ],
                cancellationToken: HttpContext.RequestAborted);

            foreach (var (key, flag) in flags)
            {
                FeatureFlags[key] = (flag, await postHogClient.IsFeatureEnabledAsync(UserId, key));
            }

            NonExistentFlag = await postHogClient.IsFeatureEnabledAsync(UserId, "non-existent-flag");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await OnGetAsync();
        if (!ApiKeyIsSet || UserId is null)
        {
            return RedirectToPage();
        }

        // Send a custom event
        postHogClient.CaptureEvent(
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

    public async Task<IActionResult> OnPostIdentifyGroupAsync()
    {
        await OnGetAsync();
        if (!ApiKeyIsSet || UserId is null)
        {
            return RedirectToPage();
        }

        // Identify a group
        var result = await postHogClient.IdentifyGroupAsync(
            Group.Type,
            Group.Key,
            Group.Name,
            properties: new()
            {
                ["size"] = "large",
                ["location"] = "San Francisco"
            },
            cancellationToken: HttpContext.RequestAborted);

        StatusMessage = result.Status == 1
            ? "Group Identified!"
            : $"Something went wrong! Status: {result.Status}";

        return RedirectToPage();
    }
}

public class GroupModel
{
    [Required]
    public string Name { get; set; } = "My Group";

    [Required]
    public string Key { get; set; } = "12345";

    [Required]
    public string Type { get; set; } = "project";
}
