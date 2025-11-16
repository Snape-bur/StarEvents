using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;

namespace StarEvents.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Homepage — preload Categories & Venues for search bar
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Venues = await _context.Venues
                .OrderBy(v => v.Name)
                .ToListAsync();

            return View();
        }

        // Search Results for Guest Users (Homepage Search)
        public async Task<IActionResult> Search(
            string? Keyword,
            int? CategoryId,
            int? VenueId,
            DateTime? DateFrom,
            DateTime? DateTo)
        {
            // Base query — only Approved events
            var q = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Where(e => e.Status == "Approved")
                .AsQueryable();

            //  Keyword Filter
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keyword = Keyword.Trim();
                q = q.Where(e =>
                    e.Title.Contains(keyword) ||
                    (e.Description != null && e.Description.Contains(keyword)) ||
                    (e.Venue != null && e.Venue.Name.Contains(keyword)) ||
                    (e.Venue != null && e.Venue.Location.Contains(keyword))
                );
            }

            //  Category Filter
            if (CategoryId.HasValue)
                q = q.Where(e => e.CategoryId == CategoryId);

            // Venue Filter
            if (VenueId.HasValue)
                q = q.Where(e => e.VenueId == VenueId);

            //  Date Range Filters
            if (DateFrom.HasValue)
                q = q.Where(e => e.StartDate >= DateFrom.Value);

            if (DateTo.HasValue)
                q = q.Where(e => e.StartDate <= DateTo.Value);

            // Load results
            var results = await q.OrderBy(e => e.StartDate).ToListAsync();

            return View("SearchResults", results);
        }
    }
}
