using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.Enums;
using System.Drawing;

namespace StarEvents.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public PaymentController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ===================================================================
        // STEP 4 : CONFIRM PAYMENT
        // ===================================================================
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null) return NotFound();

            // EXPIRED CHECK
            if (booking.ReservationExpiresAt < DateTime.UtcNow)
            {
                booking.Status = "Expired";
                booking.PaymentStatus = PaymentStatus.Cancelled;
                booking.Event.AvailableSeats += booking.Quantity;
                await _context.SaveChangesAsync();

                TempData["Error"] = "Your reservation expired. Please book again.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Already paid
            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                TempData["Ok"] = "Already paid.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // --------------------------------------------
            // ========== Loyalty Points Adjustments ==========
            // --------------------------------------------
            var lp = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (lp == null)
            {
                lp = new LoyaltyPoint
                {
                    UserId = user.Id,
                    Points = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.LoyaltyPoints.Add(lp);
                await _context.SaveChangesAsync();
            }

            // 1. Deduct redeemed points (IMPORTANT FIX)
            var redeemed = booking.PointsRedeemed ?? 0;

            if (redeemed > 0)
            {
                lp.Points = Math.Max(0, lp.Points - redeemed);
            }

            // 2. Earn new points
            int earned = (int)(booking.FinalPrice / 100m); // 1 point = ฿100
            lp.Points += earned;
            lp.LastUpdated = DateTime.UtcNow;

            // ---------------------------------------------
            // ========== Update Booking ==========
            // ---------------------------------------------
            booking.Status = "Paid";
            booking.PaymentStatus = PaymentStatus.Paid;

            // ---------------------------------------------
            // ========== Generate QR Code ==========
            // ---------------------------------------------
            string qrFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qrcodes");
            Directory.CreateDirectory(qrFolder);

            string qrPath = Path.Combine(qrFolder, $"Booking_{booking.BookingId}.png");

            using (var qrGen = new QRCodeGenerator())
            {
                var qrData = qrGen.CreateQrCode(
                    $"Booking:{booking.BookingId}|User:{user.Email}|Event:{booking.Event.Title}",
                    QRCodeGenerator.ECCLevel.Q);

                using var qrCode = new QRCode(qrData);
                using Bitmap bmp = qrCode.GetGraphic(20);
                bmp.Save(qrPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            booking.QRCodePath = $"/qrcodes/Booking_{booking.BookingId}.png";

            await _context.SaveChangesAsync();

            TempData["Ok"] = $"Payment successful! +{earned} loyalty points earned.";

            return RedirectToAction("Success", new { id = booking.BookingId });
        }

        // ===================================================================
        // STEP 5 : PAYMENT SUCCESS PAGE
        // ===================================================================
        public async Task<IActionResult> Success(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null) return NotFound();

            return View(booking);
        }
    }
}
