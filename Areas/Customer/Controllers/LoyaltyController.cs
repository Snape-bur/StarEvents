using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.ViewModels;

namespace StarEvents.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class LoyaltyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LoyaltyController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lp = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            int currentPoints = lp?.Points ?? 0;

            var transactions = await _context.LoyaltyTransactions
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Event)
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50) // last 50 actions
                .ToListAsync();

            var vm = new LoyaltyHistoryVM
            {
                CurrentPoints = currentPoints,
                Transactions = transactions
            };

            return View(vm);
        }
    }
}
