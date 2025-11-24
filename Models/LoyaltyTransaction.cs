using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEvents.Models.Enums;

namespace StarEvents.Models
{
    public class LoyaltyTransaction
    {
        [Key]
        public int LoyaltyTransactionId { get; set; }

        // User
        [Required]
        public string UserId { get; set; } = null!;
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; } = null!;

        // Related booking (optional)
        public int? BookingId { get; set; }
        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        // Positive for earn, negative for redeem
        public int PointsChange { get; set; }

        public LoyaltyTransactionType Type { get; set; }

        [MaxLength(250)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
