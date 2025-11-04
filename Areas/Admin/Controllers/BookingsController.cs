using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;

namespace StarEvents.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Index(string status, int? eventId)
        {
            var query = _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(b => b.Status == status);

            if (eventId.HasValue)
                query = query.Where(b => b.EventId == eventId);

            // Pass dropdown data
            ViewBag.Events = await _context.Events
                .OrderBy(e => e.Title)
                .ToListAsync();

            ViewBag.StatusList = new List<string> { "All", "Pending", "Paid", "Cancelled" };
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedEventId = eventId;

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }


        // GET: Admin/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }
    }
}
