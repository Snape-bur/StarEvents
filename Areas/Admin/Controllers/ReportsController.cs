using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;

namespace StarEvents.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Monthly Revenue
            var monthlyRevenue = await _context.Bookings
     .Where(b => b.Status == "Paid")
     .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
     .Select(g => new
     {
         Year = g.Key.Year,
         Month = g.Key.Month,
         Total = g.Sum(e => e.TotalPrice)
     })
     .OrderByDescending(x => x.Year)
     .ThenByDescending(x => x.Month)
     .ToListAsync();

            // ✅ Perform string formatting AFTER ToListAsync()
            var formattedRevenue = monthlyRevenue
                .Select(x => new
                {
                    Month = $"{x.Month:D2}/{x.Year}",
                    Total = x.Total
                })
                .ToList();


            // Total Counts
            var totalBookings = await _context.Bookings.CountAsync();
            var totalPaid = await _context.Bookings.CountAsync(b => b.Status == "Paid");
            var totalCancelled = await _context.Bookings.CountAsync(b => b.Status == "Cancelled");
            var totalPending = await _context.Bookings.CountAsync(b => b.Status == "Pending");
            var totalRevenue = await _context.Bookings.Where(b => b.Status == "Paid").SumAsync(b => b.TotalPrice);

            // Top 5 Events
            var topEvents = await _context.Bookings
                .Where(b => b.Status == "Paid")
                .GroupBy(b => b.Event.Title)
                .Select(g => new { Event = g.Key, Sales = g.Sum(b => b.TotalPrice) })
                .OrderByDescending(g => g.Sales)
                .Take(5)
                .ToListAsync();

            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalCancelled = totalCancelled;
            ViewBag.TotalPending = totalPending;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TopEvents = topEvents;

            return View();
        }
    }
}
