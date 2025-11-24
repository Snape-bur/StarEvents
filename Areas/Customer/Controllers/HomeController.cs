using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.ViewModels;

namespace StarEvents.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ Customer dashboard
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // ⭐ Loyalty points
            var lp = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            int loyaltyPoints = lp?.Points ?? 0;

            // ⭐ Upcoming events (only future — expired events hidden)
            var upcomingEvents = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .Where(e =>
                    e.Status == "Approved" &&
                    e.EndDate >= DateTime.UtcNow        // ⬅️ FILTER OUT EXPIRED EVENTS
                )
                .OrderBy(e => e.StartDate)
                .Take(9)
                .ToListAsync();

            // ⭐ Recent paid bookings
            var recentBookings = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Where(b => b.CustomerId == user.Id && b.Status == "Paid")
                .OrderByDescending(b => b.BookingDate)
                .Take(3)
                .ToListAsync();

            // Greeting
            var firstName = (user.FullName ?? user.Email ?? "There")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "There";

            var vm = new CustomerHomeVM
            {
                FirstName = firstName,
                LoyaltyPoints = loyaltyPoints,
                UpcomingEvents = upcomingEvents,
                RecentBookings = recentBookings
            };

            return View(vm);
        }

        // ✅ Event details page (filter expired events too)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e =>
                    e.EventId == id &&
                    e.Status == "Approved" &&
                    e.EndDate >= DateTime.UtcNow          // ⬅️ EXCLUDE EXPIRED
                );

            if (ev == null) return NotFound();

            return View(ev);
        }
    }
}
