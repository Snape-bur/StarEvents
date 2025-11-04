using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;

namespace StarEvents.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Basic totals
            var totalEvents = await _context.Events.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var totalRevenue = await _context.Bookings.SumAsync(b => (decimal?)b.TotalPrice) ?? 0;
            var totalOrganizers = await _context.Users
                .CountAsync(u => _context.UserRoles.Any(r => r.UserId == u.Id));

            var pendingEvents = await _context.Events
                .Where(e => e.Status == "Pending")
                .CountAsync();

            // Recent activity
            var recentBookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .ToListAsync();

            var recentEvents = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalEvents = totalEvents;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingEvents = pendingEvents;
            ViewBag.TotalOrganizers = totalOrganizers;
            ViewBag.RecentBookings = recentBookings;
            ViewBag.RecentEvents = recentEvents;

            return View();
        }
    }
}
