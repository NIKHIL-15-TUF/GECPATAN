using  GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace GECPatan.Core.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            // ── SEED ALL ROLES ────────────────────────────────
            foreach (var role in AppRoles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ── SEED SUPERADMIN ACCOUNT ───────────────────────
            var adminEmail = configuration["SuperAdmin:Email"]
                             ?? "admin@gecpatan.ac.in";
            var adminPassword = configuration["SuperAdmin:Password"]
                                ?? "Admin@123456";
            var adminName = configuration["SuperAdmin:Name"]
                            ?? "Super Admin";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = adminName,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, AppRoles.SuperAdmin);
                }
            }
            else
            {
                // Make sure existing admin has SuperAdmin role
                if (!await userManager.IsInRoleAsync(existingAdmin, AppRoles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(existingAdmin, AppRoles.SuperAdmin);
                }
            }
        }
    }
}