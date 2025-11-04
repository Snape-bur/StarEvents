using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Models;

namespace StarEvents.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ✅ Display all users (with role filter)
        public async Task<IActionResult> Index(string role)
        {
            var users = _userManager.Users.AsQueryable();

            // Get user list
            var userList = await users.ToListAsync();

            // Build dictionary of user roles
            var userRoles = new Dictionary<string, string>();
            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "—";
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.SelectedRole = role;

            // Apply role filter
            if (!string.IsNullOrEmpty(role) && role != "All")
            {
                userList = userList
                    .Where(u => userRoles[u.Id] == role)
                    .ToList();
            }

            return View(userList);
        }

        // ✅ Soft delete or disable user
        [HttpPost]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Lock or unlock account
            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
            {
                user.LockoutEnd = null; // unlock
            }
            else
            {
                user.LockoutEnd = DateTime.UtcNow.AddYears(100); // lock indefinitely
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // ✅ Optional: Delete user
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}
