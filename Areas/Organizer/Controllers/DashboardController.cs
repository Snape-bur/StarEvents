using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var organizer = await _userManager.GetUserAsync(User);
            if (organizer == null)
                return Unauthorized();

            // ✅ Organizer overall stats
            var events = _context.Events.Where(e => e.OrganizerId == organizer.Id);
            ViewBag.TotalEvents = await events.CountAsync();
            ViewBag.PendingEvents = await events.CountAsync(e => e.Status == "Pending");
            ViewBag.TotalBookings = await _context.Bookings.CountAsync(b => b.Event.OrganizerId == organizer.Id);
            ViewBag.TotalRevenue = await _context.Bookings
                .Where(b => b.Event.OrganizerId == organizer.Id && b.Status == "Paid")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

            // ✅ Per-event details
            var eventStats = await events
                .Include(e => e.Bookings)
                .Select(e => new OrganizerEventSummary
                {
                    Title = e.Title,
                    StartDate = e.StartDate,
                    Status = e.Status,
                    TicketPrice = e.TicketPrice,
                    TotalTickets = e.Bookings.Count(),
                    PaidTickets = e.Bookings.Count(b => b.Status == "Paid"),
                    TotalRevenue = e.Bookings.Where(b => b.Status == "Paid").Sum(b => (decimal?)b.TotalPrice) ?? 0
                })
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return View(eventStats);
        }
    }

    // ✅ Helper DTO class
    public class OrganizerEventSummary
    {
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; }
        public decimal TicketPrice { get; set; }
        public int TotalTickets { get; set; }
        public int PaidTickets { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
