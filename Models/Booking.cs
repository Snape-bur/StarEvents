using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        // 🔹 Event link
        [Required]
        public int EventId { get; set; }
        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        // 🔹 Linked user (Customer)
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        [ForeignKey(nameof(CustomerId))]
        public AppUser? Customer { get; set; }

        // 🔹 Ticket details
        [Range(1, 20)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // 🔹 Booking state
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        // 🔹 Optional digital ticket / QR
        [StringLength(250)]
        public string? QRCodePath { get; set; }
    }
}
