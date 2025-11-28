using MeetUpPlaner.Data;
using MeetUpPlaner.Models;
using MeetUpPlaner.Services.Interfaces;
using MeetupPlanner.Data;
using Microsoft.EntityFrameworkCore;

namespace MeetUpPlaner.Services
{
    public class RsvpService : IRsvpService
    {
        private readonly ApplicationDbContext _db;

        public RsvpService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> HasRsvpAsync(int eventId, string userId)
        {
            return await _db.Rsvps.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        }

        public async Task<List<Rsvp>> GetRsvpsForEventAsync(int eventId)
        {
            return await _db.Rsvps.Where(r => r.EventId == eventId).ToListAsync();
        }

        /// Create an RSVP if one doesn't exist and capacity allows.
        /// Returns Failure if duplicate or event is full (for Going).
        public async Task<OperationResult> CreateRsvpAsync(int eventId, string userId, RsvpStatus status)
        {
            // Validate
            if (string.IsNullOrEmpty(userId)) return OperationResult.Failure("UserId required.");

            // Use transaction to avoid race conditions updating counts
            using var tx = await _db.Database.BeginTransactionAsync();

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return OperationResult.Failure("Event not found.");

            var existing = await _db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
            if (existing != null) return OperationResult.Failure("RSVP already exists.");

            if (status == RsvpStatus.Going && ev.Capacity > 0 && ev.CurrentAttendeeCount >= ev.Capacity)
            {
                return OperationResult.Failure("Event is full.");
            }

            var rsvp = new Rsvp
            {
                EventId = eventId,
                UserId = userId,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _db.Rsvps.Add(rsvp);

            if (status == RsvpStatus.Going)
            {
                ev.CurrentAttendeeCount++;
                _db.Events.Update(ev);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return OperationResult.Success();
        }

        public async Task<OperationResult> CancelRsvpAsync(int eventId, string userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            var rsvp = await _db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
            if (rsvp == null) return OperationResult.Failure("RSVP not found.");

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return OperationResult.Failure("Event not found.");

            if (rsvp.Status == RsvpStatus.Going && ev.CurrentAttendeeCount > 0)
            {
                ev.CurrentAttendeeCount--;
                _db.Events.Update(ev);
            }

            _db.Rsvps.Remove(rsvp);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return OperationResult.Success();
        }

        public async Task<OperationResult> ChangeRsvpStatusAsync(int eventId, string userId, RsvpStatus newStatus)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            var rsvp = await _db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
            if (rsvp == null) return OperationResult.Failure("RSVP not found.");

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null) return OperationResult.Failure("Event not found.");

            if (rsvp.Status != RsvpStatus.Going && newStatus == RsvpStatus.Going)
            {
                if (ev.Capacity > 0 && ev.CurrentAttendeeCount >= ev.Capacity)
                    return OperationResult.Failure("Event is full.");
                ev.CurrentAttendeeCount++;
            }

            if (rsvp.Status == RsvpStatus.Going && newStatus != RsvpStatus.Going && ev.CurrentAttendeeCount > 0)
            {
                ev.CurrentAttendeeCount--;
            }

            rsvp.Status = newStatus;
            _db.Rsvps.Update(rsvp);
            _db.Events.Update(ev);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return OperationResult.Success();
        }
    }
}