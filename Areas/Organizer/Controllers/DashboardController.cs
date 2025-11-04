using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name;

            var organizer = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (organizer == null)
                return Unauthorized();

            // Organizer statistics
            var events = _context.Events
                .Where(e => e.OrganizerId == organizer.Id);

            var totalEvents = await events.CountAsync();
            var pendingEvents = await events.CountAsync(e => e.Status == "Pending");
            var totalBookings = await _context.Bookings
                .CountAsync(b => b.Event.OrganizerId == organizer.Id);
            var totalRevenue = await _context.Bookings
                .Where(b => b.Event.OrganizerId == organizer.Id && b.Status == "Paid")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

            ViewBag.TotalEvents = totalEvents;
            ViewBag.PendingEvents = pendingEvents;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }
    }
}
