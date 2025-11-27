using Bogus;
using MeetUpPlaner.Data;
using MeetUpPlaner.Models;
using MeetupPlanner.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeetUpPlaner.Data
{
    /// <summary>
    /// Generates random demo users, events and RSVPs for development use.
    /// Usage: register as scoped service and call SeedRandomAsync from startup (Development only).
    /// Respects existing counts and will only add missing items up to the requested target.
    /// </summary>
    public class RandomDataSeeder
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RandomDataSeeder> _logger;
        private readonly IConfiguration _config;

        public RandomDataSeeder(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            ILogger<RandomDataSeeder> logger,
            IConfiguration config)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Seeds random data. Default: 100 users, 20 events, rsvpChance 0.08 per user-event pair.
        /// </summary>
        public async Task SeedRandomAsync(int targetUsers = 100, int targetEvents = 20, double rsvpChance = 0.08)
        {
            // Apply migrations
            await _db.Database.MigrateAsync();

            // Read overrides from config if present
            if (int.TryParse(_config["Seed:Users"], out var cfgUsers)) targetUsers = cfgUsers;
            if (int.TryParse(_config["Seed:Events"], out var cfgEvents)) targetEvents = cfgEvents;
            if (double.TryParse(_config["Seed:RsvpChance"], out var cfgChance)) rsvpChance = cfgChance;

            // Password for all seeded users (dev only). Keep simple; do not use in prod.
            var defaultPassword = _config["Seed:Password"] ?? "P@ssw0rd1";

            // 1) Seed users (only add up to target)
            var existingUserCount = await _db.Users.CountAsync();
            var usersToCreate = Math.Max(0, targetUsers - existingUserCount);
            _logger.LogInformation("Existing users: {Existing}, target: {Target}, creating: {ToCreate}", existingUserCount, targetUsers, usersToCreate);

            var faker = new Faker("en");

            var createdUsers = new List<IdentityUser>();
            for (int i = 0; i < usersToCreate; i++)
            {
                // Guarantee uniqueness by composing a predictable email when necessary
                string email;
                int attempt = 0;
                do
                {
                    email = faker.Internet.Email().ToLowerInvariant();
                    attempt++;
                } while (await _userManager.FindByEmailAsync(email) != null && attempt < 5);

                // fallback to deterministic email
                if (await _userManager.FindByEmailAsync(email) != null)
                {
                    email = $"user{existingUserCount + i + 1}@example.com";
                }

                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, defaultPassword);
                if (result.Succeeded)
                {
                    createdUsers.Add(user);
                }
                else
                {
                    _logger.LogWarning("Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Refresh users list (only minimal info needed)
            var users = await _db.Users.OrderBy(u => u.Id).ToListAsync();

            // 2) Seed events (only add up to target)
            var existingEventCount = await _db.Events.CountAsync();
            var eventsToCreate = Math.Max(0, targetEvents - existingEventCount);
            _logger.LogInformation("Existing events: {Existing}, target: {Target}, creating: {ToCreate}", existingEventCount, targetEvents, eventsToCreate);

            var createdEvents = new List<Event>();
            for (int i = 0; i < eventsToCreate; i++)
            {
                var title = faker.Company.CatchPhrase() + " Meetup";
                var description = faker.Lorem.Paragraphs(1);
                // random date between -7 days and +60 days
                var start = faker.Date.Soon(60).AddHours(faker.Random.Int(-6, 6)); var capacity = faker.Random.Bool(0.2f) ? 0 : faker.Random.Int(5, 100);
                var creator = users[faker.Random.Int(0, users.Count - 1)];

                var ev = new Event
                {
                    Title = title,
                    Description = description,
                    StartDate = start.ToUniversalTime(),
                    EndDate = start.AddHours(faker.Random.Int(1, 4)).ToUniversalTime(),
                    Location = faker.Address.City(),
                    Capacity = capacity,
                    CreatedByUserId = creator.Id,
                    CurrentAttendeeCount = 0,
                    Status = EventStatus.Published
                };

                _db.Events.Add(ev);
                createdEvents.Add(ev);
            }

            await _db.SaveChangesAsync();

            var allEvents = await _db.Events.OrderBy(e => e.Id).ToListAsync();
            users = await _db.Users.OrderBy(u => u.Id).ToListAsync(); // refresh

            // 3) Seed random RSVPs
            // If DB already has RSVPs, we only add new ones; we won't remove existing.
            var existingRsvpCount = await _db.Rsvps.CountAsync();
            _logger.LogInformation("Existing RSVPs: {Existing}", existingRsvpCount);

            var rand = new Random();
            int addedRsvps = 0;

            // For performance, sample a subset of user-event pairs rather than iterating all combinations if very large
            var maxChecks = users.Count * allEvents.Count;
            var desiredChecks = Math.Min(maxChecks, Math.Max(1000, (int)(users.Count * allEvents.Count * rsvpChance * 1.8))); // heuristic

            for (int c = 0; c < desiredChecks; c++)
            {
                var user = users[rand.Next(users.Count)];
                var ev = allEvents[rand.Next(allEvents.Count)];

                // Decide randomly whether this pair should have an RSVP
                if (rand.NextDouble() > rsvpChance) continue;

                // Skip past events
                if (ev.StartDate < DateTime.UtcNow.AddDays(-1)) continue;

                // Skip if existing RSVP
                var exists = await _db.Rsvps.AnyAsync(r => r.EventId == ev.Id && r.UserId == user.Id);
                if (exists) continue;

                // Choose status: Going more likely than Interested/NotGoing
                var p = rand.NextDouble();
                RsvpStatus status = p < 0.6 ? RsvpStatus.Going : (p < 0.9 ? RsvpStatus.Interested : RsvpStatus.NotGoing);

                // Capacity check for Going
                if (status == RsvpStatus.Going && ev.Capacity > 0 && ev.CurrentAttendeeCount >= ev.Capacity)
                {
                    // cannot add Going for full event; maybe add Interested instead with some probability
                    if (rand.NextDouble() < 0.4)
                    {
                        status = RsvpStatus.Interested;
                    }
                    else
                    {
                        continue;
                    }
                }

                var rsvp = new Rsvp
                {
                    EventId = ev.Id,
                    UserId = user.Id,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    Note = null
                };

                _db.Rsvps.Add(rsvp);

                if (status == RsvpStatus.Going)
                {
                    ev.CurrentAttendeeCount++;
                    _db.Events.Update(ev);
                }

                addedRsvps++;
                if (addedRsvps % 200 == 0)
                {
                    await _db.SaveChangesAsync(); // flush periodically
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Random seeding complete. Users added: {UsersAdded}, Events added: {EventsAdded}, RSVPs added approx: {RsvpsAdded}", createdUsers.Count, createdEvents.Count, addedRsvps);
        }
    }
}