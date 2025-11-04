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
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Organizer/Bookings
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name;
            var organizer = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (organizer == null)
                return Unauthorized();

            // Retrieve only bookings linked to the organizer’s events
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .Where(b => b.Event.OrganizerId == organizer.Id)
                .OrderByDescending(b => b.BookingDate)
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
