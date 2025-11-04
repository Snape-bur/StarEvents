using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarEvents.Data;
using StarEvents.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;


namespace StarEvents.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ Step 1: Show booking form
        public async Task<IActionResult> Create(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id && e.Status == "Approved");

            if (ev == null) return NotFound();

            return View(ev);
        }

        // ✅ Step 2: Handle booking form submission
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int eventId, int quantity)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId && e.Status == "Approved");
            if (ev == null) return NotFound();

            if (quantity < 1 || quantity > ev.AvailableSeats)
            {
                ModelState.AddModelError("", "Invalid quantity selected.");
                return View(ev);
            }

            var customer = await _userManager.GetUserAsync(User);
            if (customer == null) return Unauthorized();

            var booking = new Booking
            {
                CustomerId = customer.Id,
                EventId = ev.EventId,
                Quantity = quantity,
                TotalPrice = quantity * ev.TicketPrice,
                Status = "Pending",
                BookingDate = DateTime.UtcNow
            };

            // Update available seats
            ev.AvailableSeats -= quantity;

            _context.Bookings.Add(booking);
            _context.Update(ev);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "✅ Booking successful!";
            return RedirectToAction(nameof(MyBookings));
        }

        // ✅ Step 3: Show customer’s bookings
        public async Task<IActionResult> MyBookings()
        {
            var customer = await _userManager.GetUserAsync(User);
            if (customer == null) return Unauthorized();

            var list = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .Where(b => b.CustomerId == customer.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(list);
        }

        // ✅ Simulate payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null)
                return NotFound();

            if (booking.Status != "Pending")
            {
                TempData["Error"] = "This booking cannot be paid again.";
                return RedirectToAction(nameof(MyBookings));
            }

            // ✅ Step 1: Mark as paid
            booking.Status = "Paid";

            // ✅ Step 2: Generate QR code (mock mode)
            string qrText = $"Booking:{booking.BookingId}|Event:{booking.Event.Title}|Customer:{user.Email}|Date:{DateTime.UtcNow:yyyy-MM-dd}";
            using var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using var bitmap = qrCode.GetGraphic(20);

            // ✅ Step 3: Save to /wwwroot/qrcodes
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/qrcodes");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, $"Booking_{booking.BookingId}.png");
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);


            // ✅ Step 4: Store path in database
            booking.QRCodePath = $"/qrcodes/Booking_{booking.BookingId}.png";

            await _context.SaveChangesAsync();

            TempData["Ok"] = $"Payment successful for {booking.Event.Title}. QR code generated!";
            return RedirectToAction(nameof(MyBookings));
        }


        // ✅ Show all paid bookings
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .Where(b => b.CustomerId == user.Id && b.Status == "Paid")
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> Ticket(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }


        [HttpGet]
        public async Task<IActionResult> DownloadTicket(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null)
                return NotFound();

            var qrPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qrcodes", $"Booking_{booking.BookingId}.png");
            if (!System.IO.File.Exists(qrPath))
                qrPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "placeholder-qr.png"); // optional fallback

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A5);
                    page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black));

                    // ===== HEADER =====
                    page.Header().Background(Colors.Blue.Medium).Padding(10).Row(row =>
                    {
                        row.RelativeColumn().AlignLeft().Text("StarEvents")
                            .SemiBold().FontColor(Colors.White).FontSize(18);
                        row.ConstantColumn(60).Height(10);
                    });

                    // ===== CONTENT =====
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Spacing(10);

                        // Event Title
                        col.Item().Text(booking.Event?.Title ?? "Event Title")
                            .Bold().FontSize(16).FontColor(Colors.Blue.Darken2);

                        // Venue and Date
                        col.Item().Text($"📍 {booking.Event?.Venue?.Name ?? "Venue"}");
                        col.Item().Text($"📅 {booking.Event?.StartDate:dd MMM yyyy hh:mm tt}");

                        // Customer Info
                        col.Item().Text($"👤 {user.FullName ?? user.Email}");
                        col.Item().Text($"💰 Total Paid: ${booking.TotalPrice:0.00}");

                        // Separator
                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        // QR Code and Info Row
                        col.Item().Row(row =>
                        {
                            row.Spacing(20);
                            row.ConstantItem(120).Height(120).Image(qrPath).FitArea();

                            row.RelativeItem().Column(inner =>
                            {
                                inner.Spacing(5);
                                inner.Item().Text("Show this ticket at the event gate.")
                                    .Italic().FontColor(Colors.Grey.Medium);
                                inner.Item().Text($"Booking ID: #{booking.BookingId}").FontSize(11);
                                inner.Item().Text($"Issued: {DateTime.UtcNow:dd MMM yyyy}").FontSize(11);
                            });
                        });
                    });

                    // ===== FOOTER =====
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("© ").FontSize(10);
                        txt.Span($"{DateTime.UtcNow.Year} StarEvents - All Rights Reserved").FontSize(10).Bold();
                    });
                });
            });

            // Output to MemoryStream
            using var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream, "application/pdf", $"Ticket_{booking.BookingId}.pdf");
        }


    }
}
