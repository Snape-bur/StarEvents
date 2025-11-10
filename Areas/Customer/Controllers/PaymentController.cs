using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.Enums;

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

        // ✅ Step 1: Show checkout page
        public async Task<IActionResult> Checkout(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null) return NotFound();
            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                TempData["Ok"] = "This booking is already paid.";
                return RedirectToAction("MyBookings", "Booking");
            }

            return View(booking);
        }

        // ✅ Step 2: Confirm and process mock payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == user.Id);

            if (booking == null) return NotFound();

            booking.PaymentStatus = PaymentStatus.Paid;
            booking.Status = "Paid";

            // reuse QR logic from your BookingController
            string qrFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/qrcodes");
            Directory.CreateDirectory(qrFolder);

            string qrPath = Path.Combine(qrFolder, $"Booking_{booking.BookingId}.png");
            using var qrGen = new QRCoder.QRCodeGenerator();
            var qrData = qrGen.CreateQrCode($"Booking:{booking.BookingId}|User:{user.Email}", QRCoder.QRCodeGenerator.ECCLevel.Q);
            var qr = new QRCoder.QRCode(qrData);
            using var bmp = qr.GetGraphic(20);
            bmp.Save(qrPath, System.Drawing.Imaging.ImageFormat.Png);
            booking.QRCodePath = $"/qrcodes/Booking_{booking.BookingId}.png";

            await _context.SaveChangesAsync();
            TempData["Ok"] = "Payment successful! Your QR ticket has been generated.";
            return RedirectToAction("MyBookings", "Booking");
        }
    }
}
