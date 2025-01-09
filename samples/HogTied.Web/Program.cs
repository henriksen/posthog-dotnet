using System.Security.Claims;
using HogTied.Web;
using HogTied.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostHog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.AddPostHog()
    .Services
    .AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString))
    .AddDatabaseDeveloperPageExceptionFilter()
    .ConfigureApplicationCookie(options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userPrincipal = context.Principal;

                Console.WriteLine($"Cookie validated for user: {userPrincipal?.Identity?.Name}");

                var userId = userPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var postHogClient = context.HttpContext.RequestServices.GetRequiredService<IPostHogClient>();
                if (userId is not null)
                {
                    await postHogClient.IdentifyAsync(userId, context.HttpContext.RequestAborted);
                }
            },
            OnSigningOut = async context =>
            {
                // This is called when the user signs out
                var userPrincipal = context.HttpContext.User;
                var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var postHogClient = context.HttpContext.RequestServices.GetRequiredService<PostHogClient>();
                if (userId is not null)
                {
                    await postHogClient.ResetAsync(userId, context.HttpContext.RequestAborted);
                }

            }
        };
    })
    .AddDefaultIdentity<IdentityUser>(
        options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages()
    .AddMvcOptions(options => options.Filters.Add<PostHogPageViewFilter>());

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
