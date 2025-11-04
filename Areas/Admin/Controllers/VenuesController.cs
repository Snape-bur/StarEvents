using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;

namespace StarEvents.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public VenuesController(ApplicationDbContext context) => _context = context;

        // GET: Admin/Venues
        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues
                .OrderBy(v => v.Name)
                .ToListAsync();
            return View(venues);
        }

        // GET: Admin/Venues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var venue = await _context.Venues.FirstOrDefaultAsync(m => m.VenueId == id);
            return venue is null ? NotFound() : View(venue);
        }

        // GET: Admin/Venues/Create
        public IActionResult Create() => View();

        // POST: Admin/Venues/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue)
        {
            if (!ModelState.IsValid)
                return View(venue);

            _context.Add(venue);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "✅ Venue created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Venues/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            return venue is null ? NotFound() : View(venue);
        }

        // POST: Admin/Venues/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue)
        {
            if (id != venue.VenueId) return NotFound();
            if (!ModelState.IsValid) return View(venue);

            try
            {
                _context.Update(venue);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "✅ Venue updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Venues.AnyAsync(v => v.VenueId == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Venues/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var venue = await _context.Venues.FirstOrDefaultAsync(m => m.VenueId == id);
            return venue is null ? NotFound() : View(venue);
        }

        // POST: Admin/Venues/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue is not null)
            {
                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "🗑 Venue deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
