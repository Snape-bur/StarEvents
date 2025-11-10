using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required, DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }

        [Required, Range(0, 1000000)]
        public decimal TicketPrice { get; set; }

        [Range(0, 1000000)]
        public int TotalSeats { get; set; }

        [Range(0, 1000000)]
        public int AvailableSeats { get; set; }

        [Required]
        public int VenueId { get; set; }
        [ForeignKey("VenueId")]
        public Venue? Venue { get; set; }

        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public string? OrganizerId { get; set; }    // later linked to AppUser
        [ForeignKey("OrganizerId")]
        public AppUser? Organizer { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "Pending"; // e.g., Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
