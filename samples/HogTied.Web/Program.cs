using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HogTied.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using PostHog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services
    .Configure<PostHogOptions>(builder.Configuration.GetSection("PostHog"))
    .AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString))
    .AddDatabaseDeveloperPageExceptionFilter()
    .ConfigureApplicationCookie(options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                // This is called every time a cookie is validated
                var userPrincipal = context.Principal;

                // Custom logic to validate the principal or add claims
                Console.WriteLine($"Cookie validated for user: {userPrincipal?.Identity?.Name}");

                // Example: Validate if the user still exists in the database
                var userId = userPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Get api key from the IOptions<PostHog>
                var postHogOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<PostHogOptions>>();
                var apiKey = postHogOptions.Value.ProjectApiKey;
                if (apiKey is not null && userId is not null)
                {
                    using var postHogClient = new PostHogClient(apiKey);
                    await postHogClient.IdentifyAsync(userId);
                }
                await Task.CompletedTask;
            }
        };
    })
    .AddDefaultIdentity<IdentityUser>(
        options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
