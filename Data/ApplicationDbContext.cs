using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StarEvents.Models;

namespace StarEvents.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<Venue> Venues { get; set; } = default!;
        public DbSet<Category> Categories { get; set; } = default!;

        public DbSet<Event> Events { get; set; } = default!;

        public DbSet<Booking> Bookings { get; set; } = default!;

        public DbSet<Discount> Discounts { get; set; } = default!;
        public DbSet<LoyaltyPoint> LoyaltyPoints { get; set; } = default!;

        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; } = null!;
    }
}
