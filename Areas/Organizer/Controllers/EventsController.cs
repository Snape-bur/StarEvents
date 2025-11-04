using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EventsController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Organizer/Events
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var events = await _context.Events
                .Where(e => e.OrganizerId == user.Id)
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }

        // GET: Organizer/Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var @event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.Id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        // GET: Organizer/Events/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "Name");
            return View();
        }

        // POST: Organizer/Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event @event)
        {
            var user = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                @event.OrganizerId = user.Id;
                @event.Status = "Pending"; // default until admin approves
                @event.CreatedAt = DateTime.UtcNow;

                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "Name", @event.VenueId);
            return View(@event);
        }

        // GET: Organizer/Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var @event = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.Id);
            if (@event == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "Name", @event.VenueId);
            return View(@event);
        }

        // POST: Organizer/Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event @event)
        {
            if (id != @event.EventId) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var existingEvent = await _context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.Id);

            if (existingEvent == null) return Unauthorized();

            if (ModelState.IsValid)
            {
                @event.OrganizerId = user.Id;
                _context.Update(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "Name", @event.VenueId);
            return View(@event);
        }

        // GET: Organizer/Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var @event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.Id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Organizer/Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var @event = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.Id);

            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
