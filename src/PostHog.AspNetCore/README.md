# PostHog.AspNetCore

This is a client SDK for the PostHog API written in C#. This package depends on the PostHog package and provides 
additional functionality for ASP.NET Core projects.

## Installation

Use the `dotnet` CLI to add the package to your project:

```bash
$ dotnet add package PostHog.AspNetCore
```

## Configuration

Register your PostHog instance in `Program.cs` (or `Startup.cs` depending on how you roll):

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddPostHog();
```

Set your project API key using user secrets:

```bash
$ dotnet user-secrets set PostHog:ProjectApiKey YOUR_API_KEY
```

Configure the PostHog client settings in `appsettings.json`:

```json
{
  ...
  "PostHog": {
    "MaxBatchSize": 100,
    "FlushAt": 10,
    "FlushInterval": "0:0:10"
  }
}
```

Inject the `IPostHogClient` interface into your controller or page:

```csharp
public class HomeController(IPostHogClient posthogClient) : Controller
{
    public IActionResult SignUpComplete()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        posthogClient.CaptureEvent(userId, "user signed up", new() { ["plan"] = "pro" });
        return View();
    }
}
```

```csharp
public class IndexModel(IPostHogClient client) : PageModel
{
    public void OnGet()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        client.CapturePageView(userId, Request.Path.Value ?? "Unknown");
    }
}
```
