using System;

namespace StarEvents.Models.ViewModels
{
    public class BookingSummaryVM
    {
        public int EventId { get; set; }

        // Event info
        public string EventTitle { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Booking info
        public int Quantity { get; set; }

        public decimal TicketPrice { get; set; }      // price per ticket (before discount)
        public decimal TotalPrice { get; set; }       // quantity * ticket price

        public string? PromoCode { get; set; }
        public decimal DiscountPercentage { get; set; } // 0 if no discount
        public decimal DiscountAmount { get; set; }     // money off
        public decimal FinalPrice { get; set; }         // TotalPrice - DiscountAmount
    }
}
