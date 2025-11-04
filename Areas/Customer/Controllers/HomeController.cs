using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;

namespace StarEvents.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Display all approved events
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .Where(e => e.Status == "Approved")
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return View(events);
        }

        // ✅ Event details page
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EventId == id && e.Status == "Approved");

            if (ev == null) return NotFound();

            return View(ev);
        }
    }
}
