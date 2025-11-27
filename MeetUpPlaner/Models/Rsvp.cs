using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeetUpPlaner.Models
{
    public class Rsvp
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [Required]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        [Required]
        public RsvpStatus Status { get; set; } = RsvpStatus.Going;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Note { get; set; }
    }

    public enum RsvpStatus
    {
        Interested = 0,
        Going = 1,
        NotGoing = 2
    }
}
