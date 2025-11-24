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
using StarEvents.Models.Enums;
using System.Drawing;

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

        // ============================================================
        // STEP 1: Checkout (Quantity + Promo + Redeem)
        // ============================================================
        public async Task<IActionResult> Checkout(int eventId)
        {
            var ev = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.Status == "Approved");

            if (ev == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // Load points
            var lp = await _context.LoyaltyPoints.FirstOrDefaultAsync(x => x.UserId == user.Id);
            ViewBag.LoyaltyPoints = lp?.Points ?? 0;

            // ✅ Count tickets already bought for THIS event by THIS user
            var alreadyBought = await _context.Bookings
                .Where(b => b.CustomerId == user.Id &&
                            b.EventId == eventId &&
                            b.Status == "Paid")
                .SumAsync(b => (int?)b.Quantity ?? 0);

            ViewBag.AlreadyBought = alreadyBought;

            return View(ev);
        }


        // ============================================================
        // STEP 2: Create Booking → Apply promo + redeem → Timer starts
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateBooking(int eventId, int quantity, string promoCode, int redeemPercent)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null) return NotFound();

            // ============================================
            // 1. MAX TICKETS LIMIT VALIDATION (IMPORTANT)
            // ============================================
            var alreadyBought = await _context.Bookings
                .Where(b => b.CustomerId == user.Id &&
                            b.EventId == eventId &&
                            b.Status == "Paid")
                .SumAsync(b => (int?)b.Quantity ?? 0);

            if (alreadyBought + quantity > 4)
            {
                TempData["Error"] = "You cannot buy more than 4 tickets for this event.";
                return RedirectToAction("Checkout", new { eventId });
            }

            // Basic quantity validation
            if (quantity < 1 || quantity > ev.AvailableSeats)
            {
                TempData["Error"] = "Invalid quantity selected.";
                return RedirectToAction("Checkout", new { eventId });
            }

            // ============================================
            // 2. CALCULATE FINAL PRICE + DISCOUNTS
            // ============================================
            decimal baseTotal = ev.TicketPrice * quantity;
            decimal finalTotal = baseTotal;

            // --- PROMO CODE ---
            decimal promoDiscount = 0;
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var promo = await _context.Discounts.FirstOrDefaultAsync(p =>
                    p.Code == promoCode &&
                    p.IsActive &&
                    p.StartDate <= DateTime.UtcNow &&
                    p.EndDate >= DateTime.UtcNow);

                if (promo != null)
                {
                    promoDiscount = baseTotal * (promo.Percentage / 100m);
                    finalTotal -= promoDiscount;
                }
            }

            // --- REDEEM POINTS ---
            var lp = await _context.LoyaltyPoints.FirstOrDefaultAsync(x => x.UserId == user.Id);
            int currentPoints = lp?.Points ?? 0;

            if (redeemPercent > 50) redeemPercent = 50;
            if (redeemPercent < 0) redeemPercent = 0;

            int requiredPoints = redeemPercent * 10; // 10% = 100 points

            if (requiredPoints > currentPoints)
            {
                redeemPercent = 0;
                requiredPoints = 0;
            }

            decimal redeemDiscount = 0;

            if (redeemPercent > 0)
            {
                redeemDiscount = finalTotal * (redeemPercent / 100m);
                finalTotal -= redeemDiscount;
            }

            // ============================================
            // 3. CREATE BOOKING
            // ============================================
            var booking = new Booking
            {
                CustomerId = user.Id,
                EventId = ev.EventId,
                Quantity = quantity,
                TotalPrice = baseTotal,
                FinalPrice = finalTotal,
                PromoCode = promoCode,
                DiscountAmount = promoDiscount + redeemDiscount,
                PointsRedeemed = requiredPoints,
                PointsDiscountAmount = redeemDiscount,
                Status = "Pending",
                PaymentStatus = PaymentStatus.Pending,
                BookingDate = DateTime.UtcNow,
                ReservationExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.Bookings.Add(booking);

            // reserve seats
            ev.AvailableSeats -= quantity;
            await _context.SaveChangesAsync();

            return RedirectToAction("Summary", new { id = booking.BookingId });
        }


        // ============================================================
        // STEP 3: Summary Page (Timer + Final Price)
        // ============================================================
        public async Task<IActionResult> Summary(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null) return NotFound();

            if (booking.ReservationExpiresAt < DateTime.UtcNow)
            {
                booking.Status = "Expired";
                booking.PaymentStatus = PaymentStatus.Cancelled;
                booking.Event.AvailableSeats += booking.Quantity;
                await _context.SaveChangesAsync();

                TempData["Error"] = "Your reservation expired.";
                return RedirectToAction("MyBookings");
            }

            return View(booking);
        }

        // ============================================================
        // CANCEL BOOKING
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null) return NotFound();

            if (booking.Status != "Pending")
            {
                TempData["Error"] = "You cannot cancel this booking.";
                return RedirectToAction("MyBookings");
            }

            booking.Status = "Cancelled";
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.Event.AvailableSeats += booking.Quantity;

            await _context.SaveChangesAsync();
            TempData["Ok"] = "Booking cancelled.";

            return RedirectToAction("MyBookings");
        }

        // ============================================================
        // MY BOOKINGS LIST
        // ============================================================
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var list = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .Where(b => b.CustomerId == user.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(list);
        }

        // ✅ History Page – Shows Paid/Completed Events
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var history = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Where(b => b.CustomerId == user.Id &&
                            b.Status == "Paid")
                .OrderByDescending(b => b.Event.StartDate)
                .ToListAsync();

            return View(history);  
        }


        // ============================================================
        // DOWNLOAD TICKET (PDF)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> DownloadTicket(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null) return NotFound();

            // QR path (fallback if missing)
            var qrPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qrcodes", $"Booking_{booking.BookingId}.png");
            if (!System.IO.File.Exists(qrPath))
            {
                qrPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "placeholder-qr.png");
            }

            // Generate PDF
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A5);
                    page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black));

                    page.Header()
                        .Background(Colors.DeepPurple.Medium)
                        .Padding(10)
                        .Text("StarEvents Ticket")
                        .FontSize(20)
                        .FontColor(Colors.White)
                        .SemiBold();

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text(booking.Event.Title).Bold().FontSize(18);
                        col.Item().Text($"Venue: {booking.Event.Venue?.Name}");
                        col.Item().Text($"Date: {booking.Event.StartDate:dd MMM yyyy hh:mm tt}");
                        col.Item().Text($"Name: {user.FullName ?? user.Email}");
                        col.Item().Text($"Paid: ฿{booking.FinalPrice:0.00}");

                        if (!string.IsNullOrWhiteSpace(booking.PromoCode))
                            col.Item().Text($"Promo Used: {booking.PromoCode}");

                        col.Item().PaddingVertical(15).Row(row =>
                        {
                            row.ConstantItem(120).Height(120).Image(qrPath);
                            row.RelativeItem().Text($"Booking ID: {booking.BookingId}");
                        });
                    });

                    page.Footer().AlignCenter().Text($"© {DateTime.UtcNow.Year} StarEvents — All rights reserved");
                });
            });

            var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream, "application/pdf", $"Ticket_{booking.BookingId}.pdf");
        }
    }
}
