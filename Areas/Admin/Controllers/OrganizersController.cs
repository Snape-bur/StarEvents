using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;

namespace StarEvents.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrganizersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public OrganizersController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Organizers
        public async Task<IActionResult> Index()
        {
            // users who are marked as organizer or in Organizer role
            var orgRoleUsers = await _userManager.GetUsersInRoleAsync("Organizer");
            var query = _context.Users
                .Where(u => u.IsOrganizer || orgRoleUsers.Select(x => x.Id).Contains(u.Id))
                .Select(u => new OrganizerRow
                {
                    Id = u.Id,
                    Email = u.Email!,
                    FullName = u.FullName,
                    Phone = u.PhoneNumber,
                    IsOrganizer = u.IsOrganizer,
                    IsActive = u.IsActive,
                    ApprovedAt = u.ApprovedAt
                })
                .OrderBy(x => x.FullName ?? x.Email);

            return View(await query.ToListAsync());
        }

        // POST: Admin/Organizers/Approve/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();

            // Ensure the Organizer role exists
            if (!await _roleManager.RoleExistsAsync("Organizer"))
                await _roleManager.CreateAsync(new IdentityRole("Organizer"));

            // Add to Organizer role if not already
            if (!await _userManager.IsInRoleAsync(user, "Organizer"))
                await _userManager.AddToRoleAsync(user, "Organizer");

            // ✅ Activate & confirm the account
            user.IsOrganizer = true;
            user.IsActive = true;
            user.EmailConfirmed = true;          // 🔥 IMPORTANT: allows login
            user.ApprovedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            // ✅ (Optional) send email notification to organizer
            // await _emailSender.SendEmailAsync(user.Email, "Account Approved",
            //     $"Hello {user.FullName}, your organizer account has been approved! You can now log in at {Url.Page("/Account/Login", new { area = "Identity" }, Request.Scheme)}.");

            TempData["Ok"] = $"✅ Organizer {user.Email} has been approved and can now log in.";
            return RedirectToAction(nameof(Index));
        }


        // POST: Admin/Organizers/Suspend/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();

            // keep role membership, but disable activity
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            TempData["Ok"] = $"Suspended organizer: {user.Email}";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Organizers/RemoveRole/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Organizer"))
                await _userManager.RemoveFromRoleAsync(user, "Organizer");

            user.IsOrganizer = false;
            user.IsActive = false;

            await _userManager.UpdateAsync(user);
            TempData["Ok"] = $"Removed Organizer role: {user.Email}";
            return RedirectToAction(nameof(Index));
        }

        public class OrganizerRow
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public bool IsOrganizer { get; set; }
            public bool IsActive { get; set; }
            public DateTime? ApprovedAt { get; set; }
        }
    }
}
