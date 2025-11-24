using System;
using System.Collections.Generic;
using StarEvents.Models;

namespace StarEvents.Models.ViewModels
{
    public class CustomerHomeVM
    {
        public string FirstName { get; set; } = string.Empty;

        public int LoyaltyPoints { get; set; }

        // Main event list for the page
        public List<Event> UpcomingEvents { get; set; } = new();

        // Small panel of recent successful bookings
        public List<Booking> RecentBookings { get; set; } = new();
    }
}
