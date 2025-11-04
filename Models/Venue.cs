using System.ComponentModel.DataAnnotations;

namespace StarEvents.Models
{
    public class Venue
    {
        [Key]                               
        public int VenueId { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(1, 100000, ErrorMessage = "Capacity must be at least 1.")]
        public int Capacity { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }
    }
}
