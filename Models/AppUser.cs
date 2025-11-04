using Microsoft.AspNetCore.Identity;
using System;

namespace StarEvents.Models
{
    public class AppUser : IdentityUser
    {
        // ✅ Common fields
        public string? FullName { get; set; }

        // ✅ Organizer-related flags
        public bool IsOrganizer { get; set; } = false;  // Marks user as organizer
        public bool IsActive { get; set; } = false;     // Must be approved by admin
        public DateTime? ApprovedAt { get; set; }       // When admin approved

        // ✅ Customer convenience flag
        public bool IsCustomer { get; set; } = true;    // Default true for new users

        // ✅ Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
