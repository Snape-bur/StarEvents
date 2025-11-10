using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarEvents.Data;
using StarEvents.Models;

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

        // ✅ Index with optional date filter
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Bookings.AsQueryable();

            // ✅ Apply date filter if provided
            if (fromDate.HasValue && toDate.HasValue)
            {
                query = query.Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate);
                ViewBag.FilterMessage = $"Showing results from {fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}";
            }
            else
            {
                ViewBag.FilterMessage = "Showing all bookings";
            }

            // ✅ Sales summary
            var totalBookings = await query.CountAsync();
            var totalPaid = await query.CountAsync(b => b.Status == "Paid");
            var totalPending = await query.CountAsync(b => b.Status == "Pending");
            var totalCancelled = await query.CountAsync(b => b.Status == "Cancelled");
            var totalRevenue = await query.Where(b => b.Status == "Paid").SumAsync(b => b.TotalPrice);

            // ✅ Monthly revenue
            var monthlyRevenue = await query
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

            var formattedRevenue = monthlyRevenue
                .Select(x => new
                {
                    Month = $"{x.Month:D2}/{x.Year}",
                    Total = x.Total
                })
                .ToList();

            // ✅ Top 5 Events
            var topEvents = await query
                .Where(b => b.Status == "Paid")
                .GroupBy(b => b.Event.Title)
                .Select(g => new { Event = g.Key, Sales = g.Sum(b => b.TotalPrice) })
                .OrderByDescending(g => g.Sales)
                .Take(5)
                .ToListAsync();

            // ✅ User & Event summary
            var totalUsers = await _context.Users.CountAsync();
            var totalAdmins = await _context.Users.CountAsync(u => u.IsActive && !u.IsCustomer && !u.IsOrganizer);
            var totalOrganizers = await _context.Users.CountAsync(u => u.IsOrganizer);
            var totalCustomers = await _context.Users.CountAsync(u => u.IsCustomer);

            var totalEvents = await _context.Events.CountAsync();
            var activeEvents = await _context.Events.CountAsync(e => e.StartDate >= DateTime.Today);
            var completedEvents = await _context.Events.CountAsync(e => e.EndDate < DateTime.Today);

            // ✅ Pass to View
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.MonthlyRevenue = formattedRevenue;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalPending = totalPending;
            ViewBag.TotalCancelled = totalCancelled;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TopEvents = topEvents;

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.TotalOrganizers = totalOrganizers;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalEvents = totalEvents;
            ViewBag.ActiveEvents = activeEvents;
            ViewBag.CompletedEvents = completedEvents;

            return View();
        }

        // ✅ Export PDF
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(DateTime? fromDate, DateTime? toDate)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var query = _context.Bookings.AsQueryable();
            if (fromDate.HasValue && toDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate);

            var totalBookings = await query.CountAsync();
            var totalPaid = await query.CountAsync(b => b.Status == "Paid");
            var totalPending = await query.CountAsync(b => b.Status == "Pending");
            var totalCancelled = await query.CountAsync(b => b.Status == "Cancelled");
            var totalRevenue = await query.Where(b => b.Status == "Paid").SumAsync(b => b.TotalPrice);

            var topEvents = await query
                .Where(b => b.Status == "Paid")
                .GroupBy(b => b.Event.Title)
                .Select(g => new { Event = g.Key, Sales = g.Sum(b => b.TotalPrice) })
                .OrderByDescending(g => g.Sales)
                .Take(5)
                .ToListAsync();

            // ✅ User & Event summary
            var totalUsers = await _context.Users.CountAsync();
            var totalAdmins = await _context.Users.CountAsync(u => u.IsActive && !u.IsCustomer && !u.IsOrganizer);
            var totalOrganizers = await _context.Users.CountAsync(u => u.IsOrganizer);
            var totalCustomers = await _context.Users.CountAsync(u => u.IsCustomer);

            var totalEvents = await _context.Events.CountAsync();
            var activeEvents = await _context.Events.CountAsync(e => e.StartDate >= DateTime.Today);
            var completedEvents = await _context.Events.CountAsync(e => e.EndDate < DateTime.Today);

            // ✅ Generate PDF
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12));
                    page.Header().Text("⭐ StarEvents - System Report").FontSize(18).Bold().FontColor(Colors.Blue.Medium);

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text(fromDate.HasValue && toDate.HasValue
                            ? $"Report Period: {fromDate:dd MMM yyyy} - {toDate:dd MMM yyyy}"
                            : "Full System Report");

                        col.Item().Text($"Total Bookings: {totalBookings}");
                        col.Item().Text($"Paid: {totalPaid}, Pending: {totalPending}, Cancelled: {totalCancelled}");
                        col.Item().Text($"Total Revenue: ฿{totalRevenue:N2}");

                        col.Item().PaddingTop(10).Text("📊 Users Overview:").Bold();
                        col.Item().Text($"Total Users: {totalUsers}");
                        col.Item().Text($"- Admins: {totalAdmins}");
                        col.Item().Text($"- Organizers: {totalOrganizers}");
                        col.Item().Text($"- Customers: {totalCustomers}");

                        col.Item().PaddingTop(10).Text("🎟 Events Summary:").Bold();
                        col.Item().Text($"Total Events: {totalEvents}");
                        col.Item().Text($"Active: {activeEvents}, Completed: {completedEvents}");

                        col.Item().PaddingTop(10).Text("🏆 Top 5 Events:").Bold();
                        foreach (var e in topEvents)
                            col.Item().Text($"{e.Event} - ฿{e.Sales:N2}");
                    });

                    page.Footer().AlignCenter().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}");
                });
            });

            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"StarEvents_Report_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
    }
}
