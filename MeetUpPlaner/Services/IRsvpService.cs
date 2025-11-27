using MeetUpPlaner.Models;

namespace MeetUpPlaner.Services.Interfaces
{
    public interface IRsvpService
    {
        Task<bool> HasRsvpAsync(int eventId, string userId);
        Task<List<Rsvp>> GetRsvpsForEventAsync(int eventId);
        Task<OperationResult> CreateRsvpAsync(int eventId, string userId, RsvpStatus status);
        Task<OperationResult> CancelRsvpAsync(int eventId, string userId);
        Task<OperationResult> ChangeRsvpStatusAsync(int eventId, string userId, RsvpStatus newStatus);
    }
}