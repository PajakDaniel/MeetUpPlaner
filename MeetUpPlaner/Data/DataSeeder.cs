using MeetUpPlaner.Data;
using MeetUpPlaner.Models;
using MeetupPlanner.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;

namespace MeetUpPlaner.Data
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DataSeeder> _logger;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public DataSeeder(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DataSeeder> logger,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _config = config;
            _env = env;
        }

        public async Task SeedAsync()
        {
            await _db.Database.MigrateAsync();

            var seededLines = new List<string>
            {
                $"Seed run at {DateTime.UtcNow:u}"
            };

            var adminRoleName = "Admin";
            if (!await _roleManager.RoleExistsAsync(adminRoleName))
            {
                var res = await _roleManager.CreateAsync(new IdentityRole(adminRoleName));
                if (res.Succeeded)
                    _logger.LogInformation("Created role '{Role}'", adminRoleName);
                else
                    _logger.LogWarning("Failed to create role '{Role}': {Errors}", adminRoleName, string.Join(", ", res.Errors.Select(e => e.Description)));
            }

            var adminEmail = _config["Seed:AdminEmail"] ?? "admin@example.com";
            var adminPassword = _config["Seed:AdminPassword"] ?? "P@ssw0rd1";

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createAdmin = await _userManager.CreateAsync(adminUser, adminPassword);
                if (createAdmin.Succeeded)
                {
                    _logger.LogInformation("Created admin user {Email}", adminEmail);
                    seededLines.Add($"ADMIN: {adminEmail} / {adminPassword}");
                }
                else
                {
                    _logger.LogWarning("Failed to create admin user {Email}: {Errors}", adminEmail, string.Join(", ", createAdmin.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                seededLines.Add($"ADMIN (existing): {adminEmail} / {adminPassword}");
            }

            // Ensure admin user in role
            if (adminUser != null && !await _userManager.IsInRoleAsync(adminUser, adminRoleName))
            {
                var addRoleRes = await _userManager.AddToRoleAsync(adminUser, adminRoleName);
                if (addRoleRes.Succeeded)
                {
                    _logger.LogInformation("Assigned user {Email} to role {Role}", adminEmail, adminRoleName);
                }
                else
                {
                    _logger.LogWarning("Failed to assign role to {Email}: {Errors}", adminEmail, string.Join(", ", addRoleRes.Errors.Select(e => e.Description)));
                }
            }

            // Add a few deterministic demo users (idempotent)
            var demoUsers = new[]
            {
                new { Email = "alice@example.com", Password = "P@ssw0rd1" },
                new { Email = "bob@example.com", Password = "P@ssw0rd1" },
                new { Email = "charlie@example.com", Password = "P@ssw0rd1" }
            };

            foreach (var u in demoUsers)
            {
                var existing = await _userManager.FindByEmailAsync(u.Email);
                if (existing == null)
                {
                    var user = new IdentityUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        EmailConfirmed = true
                    };
                    var res = await _userManager.CreateAsync(user, u.Password);
                    if (res.Succeeded)
                    {
                        _logger.LogInformation("Created demo user {Email}", u.Email);
                        seededLines.Add($"USER: {u.Email} / {u.Password}");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create demo user {Email}: {Errors}", u.Email, string.Join(", ", res.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    seededLines.Add($"USER (existing): {u.Email} / {u.Password}");
                }
            }

            try
            {
                var outPath = Path.Combine(_env.ContentRootPath, "seeded-users.txt");
                await File.WriteAllLinesAsync(outPath, seededLines, Encoding.UTF8);
                _logger.LogInformation("Wrote seeded users to {Path}", outPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to write seeded-users.txt: {Error}", ex.Message);
            }
        }
    }
}