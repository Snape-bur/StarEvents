using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.ViewModels;

namespace StarEvents.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ List + Search + Filter Events
        public async Task<IActionResult> Index([FromQuery] EventBrowseVM vm)
        {
            // Populate dropdowns
            vm.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToListAsync();

            vm.Venues = await _context.Venues
                .OrderBy(v => v.Name)
                .Select(v => new SelectListItem
                {
                    Value = v.VenueId.ToString(),
                    Text = v.Name
                }).ToListAsync();

            // Base query (Approved + Not Expired)
            var q = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Where(e =>
                    e.Status == "Approved" &&
                    e.EndDate >= DateTime.UtcNow          // ⭐ HIDE EXPIRED EVENTS
                )
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(vm.Keyword))
            {
                string keyword = vm.Keyword.Trim();
                q = q.Where(e =>
                    e.Title.Contains(keyword) ||
                    (e.Description != null && e.Description.Contains(keyword)) ||
                    (e.Venue != null && e.Venue.Name.Contains(keyword)) ||
                    (e.Venue != null && e.Venue.Location.Contains(keyword))
                );
            }

            if (vm.CategoryId.HasValue)
                q = q.Where(e => e.CategoryId == vm.CategoryId);

            if (vm.VenueId.HasValue)
                q = q.Where(e => e.VenueId == vm.VenueId);

            if (vm.DateFrom.HasValue)
                q = q.Where(e => e.StartDate >= vm.DateFrom.Value);

            if (vm.DateTo.HasValue)
                q = q.Where(e => e.StartDate <= vm.DateTo.Value);

            // Load results
            vm.Results = await q.OrderBy(e => e.StartDate).ToListAsync();

            vm.Discounts = await _context.Discounts
                .Where(d => d.IsActive &&
                            d.StartDate <= DateTime.UtcNow &&
                            d.EndDate >= DateTime.UtcNow)
                .ToListAsync();

            return View(vm);
        }

        // ✅ Event details page
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e =>
                    e.EventId == id &&
                    e.Status == "Approved" &&
                    e.EndDate >= DateTime.UtcNow     // ⭐ Prevent viewing expired event
                );

            if (ev == null) return NotFound();

            return View(ev);
        }
    }
}
