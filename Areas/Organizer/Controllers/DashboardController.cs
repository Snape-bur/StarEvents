using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using StarEvents.Data;
using StarEvents.Models;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;



namespace StarEvents.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var organizer = await _userManager.GetUserAsync(User);
            if (organizer == null)
                return Unauthorized();

            // ✅ Organizer overall stats
            var events = _context.Events.Where(e => e.OrganizerId == organizer.Id);
            ViewBag.TotalEvents = await events.CountAsync();
            ViewBag.PendingEvents = await events.CountAsync(e => e.Status == "Pending");
            ViewBag.TotalBookings = await _context.Bookings.CountAsync(b => b.Event.OrganizerId == organizer.Id);
            ViewBag.TotalRevenue = await _context.Bookings
                .Where(b => b.Event.OrganizerId == organizer.Id && b.Status == "Paid")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

            // ✅ Per-event details
            var eventStats = await events
                .Include(e => e.Bookings)
                .Select(e => new OrganizerEventSummary
                {
                    Title = e.Title,
                    StartDate = e.StartDate,
                    Status = e.Status,
                    TicketPrice = e.TicketPrice,
                    TotalTickets = e.Bookings.Count(),
                    PaidTickets = e.Bookings.Count(b => b.Status == "Paid"),
                    TotalRevenue = e.Bookings.Where(b => b.Status == "Paid").Sum(b => (decimal?)b.TotalPrice) ?? 0
                })
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return View(eventStats);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            var organizer = await _userManager.GetUserAsync(User);
            if (organizer == null)
                return Unauthorized();

            // Load event stats again
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizer.Id)
                .Include(e => e.Bookings)
                .ToListAsync();

            var summary = events
                .Select(e => new OrganizerEventSummary
                {
                    Title = e.Title,
                    StartDate = e.StartDate,
                    Status = e.Status,
                    TicketPrice = e.TicketPrice,
                    TotalTickets = e.Bookings.Count(),
                    PaidTickets = e.Bookings.Count(b => b.Status == "Paid"),
                    TotalRevenue = e.Bookings.Where(b => b.Status == "Paid")
                                             .Sum(b => (decimal?)b.TotalPrice) ?? 0
                })
                .OrderByDescending(e => e.StartDate)
                .ToList();

            // PDF document
            QuestPDF.Settings.License = LicenseType.Community;
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Text($"Event Performance Summary")
                        .FontSize(18).Bold();

                    page.Content().Column(col =>
                    {
                        col.Spacing(12);

                        // Global Summary
                        col.Item().Text($"Total Events: {summary.Count}").Bold();
                        col.Item().Text($"Total Tickets Sold: {summary.Sum(x => x.TotalTickets)}").Bold();
                        col.Item().Text($"Total Revenue: ${summary.Sum(x => x.TotalRevenue):0.00}").Bold();

                        col.Item().LineHorizontal(1);

                        // Table header
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Text("Event").Bold();
                            row.RelativeColumn().Text("Date").Bold();
                            row.RelativeColumn().Text("Tickets").Bold();
                            row.RelativeColumn().Text("Paid").Bold();
                            row.RelativeColumn().Text("Revenue").Bold();
                            row.RelativeColumn().Text("Status").Bold();
                        });

                        col.Item().LineHorizontal(1);

                        // Table rows
                        foreach (var e in summary)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text(e.Title);
                                row.RelativeColumn().Text(e.StartDate.ToString("dd MMM yyyy"));
                                row.RelativeColumn().Text(e.TotalTickets.ToString());
                                row.RelativeColumn().Text(e.PaidTickets.ToString());
                                row.RelativeColumn().Text($"${e.TotalRevenue:0.00}");
                                row.RelativeColumn().Text(e.Status);
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text($"Generated on: {DateTime.Now:dd MMM yyyy HH:mm}");
                });
            });

            var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream, "application/pdf", "OrganizerEventSummary.pdf");
        }

    }



    // ✅ Helper DTO class
    public class OrganizerEventSummary
    {
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; }
        public decimal TicketPrice { get; set; }
        public int TotalTickets { get; set; }
        public int PaidTickets { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
