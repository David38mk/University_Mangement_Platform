using Microsoft.AspNetCore.Identity;
using University_Management_Platform.Areas.Identity.Data;

namespace University_Management_Platform.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<University_Management_PlatformUser>>();

            const string adminRole = "Admin";
            const string adminEmail = "admin@university.com";
            const string adminPassword = "Admin123!"; // change for production

            // Ensure role exists
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(adminRole));
                if (!roleResult.Succeeded)
                {
                    throw new Exception("Failed to create Admin role: " +
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            // Ensure user exists
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new University_Management_PlatformUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var userResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!userResult.Succeeded)
                {
                    throw new Exception("Failed to create Admin user: " +
                        string.Join(", ", userResult.Errors.Select(e => e.Description)));
                }
            }

            // Ensure user is in role
            if (!await userManager.IsInRoleAsync(adminUser, adminRole))
            {
                var addRoleResult = await userManager.AddToRoleAsync(adminUser, adminRole);
                if (!addRoleResult.Succeeded)
                {
                    throw new Exception("Failed to assign Admin role: " +
                        string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
