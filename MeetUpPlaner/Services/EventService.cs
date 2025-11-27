using MeetUpPlaner.Data;
using MeetUpPlaner.Models;
using MeetUpPlaner.Services.Interfaces;
using MeetupPlanner.Data;
using Microsoft.EntityFrameworkCore;

namespace MeetUpPlaner.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _db;

        public EventService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            return await _db.Events.FindAsync(id);
        }

        public async Task<List<Event>> GetUpcomingAsync(int daysAhead = 30)
        {
            var until = DateTime.UtcNow.AddDays(daysAhead);
            return await _db.Events
                .Where(e => e.Status == EventStatus.Published && e.StartDate <= until && e.StartDate >= DateTime.UtcNow.AddDays(-1))
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<OperationResult> CreateEventAsync(Event ev, string createdByUserId)
        {
            if (ev == null) return OperationResult.Failure("Event cannot be null.");
            if (string.IsNullOrEmpty(ev.Title)) return OperationResult.Failure("Title is required.");

            ev.CreatedByUserId = createdByUserId;
            ev.CurrentAttendeeCount = 0;
            ev.Status = ev.Status == 0 ? EventStatus.Published : ev.Status;

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            return OperationResult.Success();
        }

        public async Task<OperationResult> UpdateEventAsync(Event ev, string currentUserId, bool isAdmin = false)
        {
            if (ev == null) return OperationResult.Failure("Event cannot be null.");

            var existing = await _db.Events.FindAsync(ev.Id);
            if (existing == null) return OperationResult.Failure("Event not found.");

            if (!isAdmin && existing.CreatedByUserId != currentUserId)
                return OperationResult.Failure("Only the owner or an admin may edit this event.");

            existing.Title = ev.Title;
            existing.Description = ev.Description;
            existing.StartDate = ev.StartDate;
            existing.EndDate = ev.EndDate;
            existing.Location = ev.Location;
            existing.Capacity = ev.Capacity;
            existing.Status = ev.Status;

            _db.Events.Update(existing);
            await _db.SaveChangesAsync();
            return OperationResult.Success();
        }

        public async Task<OperationResult> DeleteEventAsync(int eventId, string currentUserId, bool isAdmin = false)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return OperationResult.Failure("Event not found.");

            if (!isAdmin && ev.CreatedByUserId != currentUserId)
                return OperationResult.Failure("Only the owner or an admin may delete this event.");

            // Remove related RSVPs explicitly to ensure referential cleanup
            var rsvps = _db.Rsvps.Where(r => r.EventId == eventId);
            _db.Rsvps.RemoveRange(rsvps);
            _db.Events.Remove(ev);

            await _db.SaveChangesAsync();
            return OperationResult.Success();
        }
    }
}