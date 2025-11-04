using Microsoft.AspNetCore.Identity;
using StarEvents.Models;

namespace StarEvents.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            string[] roles = { "Admin", "Organizer", "Customer" };
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            // ✅ Default Admin Account
            var adminEmail = "admin@starevents.com";
            var admin = await userMgr.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                var user = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true // ✅ ensures admin is active by default
                };
                var result = await userMgr.CreateAsync(user, "Admin@123");
                if (result.Succeeded)
                    await userMgr.AddToRoleAsync(user, "Admin");
            }

            // ✅ Optional: Default Organizer (for testing)
            var orgEmail = "organizer@starevents.com";
            var organizer = await userMgr.FindByEmailAsync(orgEmail);
            if (organizer == null)
            {
                var orgUser = new AppUser
                {
                    UserName = orgEmail,
                    Email = orgEmail,
                    FullName = "Default Organizer",
                    EmailConfirmed = true,
                    IsOrganizer = true,
                    IsActive = true,
                    ApprovedAt = DateTime.UtcNow
                };
                var result = await userMgr.CreateAsync(orgUser, "Organizer@123");
                if (result.Succeeded)
                    await userMgr.AddToRoleAsync(orgUser, "Organizer");
            }
        }
    }
}
