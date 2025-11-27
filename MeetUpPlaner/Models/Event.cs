using System.ComponentModel.DataAnnotations;

namespace MeetUpPlaner.Models
{
    public class Event
    {

        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = "";

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Range(0, int.MaxValue)]
        public int Capacity { get; set; } = 0;

        public string? CreatedByUserId { get; set; }

        public int CurrentAttendeeCount { get; set; } = 0;

        public EventStatus Status { get; set; } = EventStatus.Published;
    }

    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        Cancelled = 2,
        Completed = 3
    }

}
