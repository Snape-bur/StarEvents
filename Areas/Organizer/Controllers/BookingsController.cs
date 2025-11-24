using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? eventId, string status, int page = 1, int pageSize = 8)
        {
            var userEmail = User.Identity?.Name;
            var organizer = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (organizer == null)
                return Unauthorized();

            // Events for dropdown
            var organizerEvents = await _context.Events
                .Where(e => e.OrganizerId == organizer.Id)
                .OrderBy(e => e.Title)
                .ToListAsync();

            ViewBag.Events = organizerEvents.Select(e => new SelectListItem
            {
                Value = e.EventId.ToString(),
                Text = e.Title
            }).ToList();

            ViewBag.SelectedEventId = eventId;
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;

            // Base query
            var query = _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .Where(b => b.Event.OrganizerId == organizer.Id);

            // Filter by event
            if (eventId.HasValue)
                query = query.Where(b => b.EventId == eventId.Value);

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                query = query.Where(b => b.Status == status);

            // Count for pagination
            var totalItems = await query.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Pagination
            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(bookings);
        }


        // GET: Organizer/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }
    }
}
