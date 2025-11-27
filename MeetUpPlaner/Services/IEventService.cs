using MeetUpPlaner.Models;

namespace MeetUpPlaner.Services.Interfaces
{
    public interface IEventService
    {
        Task<Event?> GetByIdAsync(int id);
        Task<List<Event>> GetUpcomingAsync(int daysAhead = 30);
        Task<OperationResult> CreateEventAsync(Event ev, string createdByUserId);
        Task<OperationResult> UpdateEventAsync(Event ev, string currentUserId, bool isAdmin = false);
        Task<OperationResult> DeleteEventAsync(int eventId, string currentUserId, bool isAdmin = false);
    }
}