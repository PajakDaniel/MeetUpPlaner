using MeetUpPlaner.Authorization;
using MeetUpPlaner.Data;
using MeetUpPlaner.Services;
using MeetUpPlaner.Services.Interfaces;
using MeetupPlanner.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=meetup.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

ConfigureServices(builder);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

static void ConfigureServices(WebApplicationBuilder builder)
{
    var services = builder.Services;

    services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

    services.AddRazorPages();

    services.AddScoped<IEventService, EventService>();
    services.AddScoped<IRsvpService, RsvpService>();

    services.AddScoped<DataSeeder>();
    services.AddScoped<RandomDataSeeder>();

    services.AddScoped<IAuthorizationHandler, RequireAdminOrOwnerHandler>();
    services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminOrOwner", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new RequireAdminOrOwnerRequirement());
        });
    });
}