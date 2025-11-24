using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEvents.Models.Enums;

namespace StarEvents.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        //  Event link
        [Required]
        public int EventId { get; set; }
        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        //  Linked user (Customer)
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        [ForeignKey(nameof(CustomerId))]
        public AppUser? Customer { get; set; }

        //  Ticket details
        [Range(1, 20)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        //  Booking state
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled, Expired

        public string? PromoCode { get; set; }
        public decimal? DiscountAmount { get; set; }

        //  Final price AFTER promo, BEFORE loyalty, then updated after loyalty
        [Column(TypeName = "decimal(10,2)")]
        public decimal FinalPrice { get; set; }

        public int? PointsRedeemed { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PointsDiscountAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        // Reservation expiry (for pending bookings)
        public DateTime? ReservationExpiresAt { get; set; }

        //Convenience property – NOT stored in DB
        [NotMapped]
        public bool IsReservationExpired =>
            ReservationExpiresAt.HasValue && DateTime.UtcNow > ReservationExpiresAt.Value;

        //  Optional digital ticket / QR
        [StringLength(250)]
        public string? QRCodePath { get; set; }
    }
}
