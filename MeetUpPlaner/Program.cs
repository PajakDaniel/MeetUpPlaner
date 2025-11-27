using MeetUpPlaner.Authorization;
using MeetUpPlaner.Data;
using MeetUpPlaner.Services;
using MeetUpPlaner.Services.Interfaces;
using MeetupPlanner.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=meetup.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity & roles (keep your existing Identity config)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.AddRazorPages();

// === Add these service registrations so DI can resolve IEventService / IRsvpService ===
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRsvpService, RsvpService>();
// ================================================================================

// Register seeders and authorization pieces if you have them
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<RandomDataSeeder>();
builder.Services.AddScoped<IAuthorizationHandler, RequireAdminOrOwnerHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminOrOwner", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new RequireAdminOrOwnerRequirement());
    });
});

var app = builder.Build();

// ... your existing startup/seeding logic (unchanged)

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();