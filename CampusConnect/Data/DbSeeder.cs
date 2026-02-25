using CampusConnect.Constants;
using CampusConnect.Models; // <-- ApplicationUser
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusConnect.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            // Resolve managers (must match Program.cs Identity types)
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILoggerFactory>()
                                 .CreateLogger("DbSeeder");

            // -----------------------------
            // Seed roles safely
            // -----------------------------
            string[] roles =
            {
                Roles.Admin.ToString(),
                Roles.User.ToString(),
                Roles.Staff.ToString(),
                Roles.Manager.ToString()
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));

                    if (!roleResult.Succeeded)
                    {
                        foreach (var err in roleResult.Errors)
                        {
                            logger.LogError("Role creation failed: {Error}", err.Description);
                        }
                    }
                }
            }

            // -----------------------------
            // Seed admin user safely
            // -----------------------------
            var email = "admin@gmail.com";
            var userInDb = await userManager.FindByEmailAsync(email);

            if (userInDb == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123");

                if (createResult.Succeeded)
                {
                    var roleAssign = await userManager.AddToRoleAsync(adminUser, Roles.Admin.ToString());

                    if (!roleAssign.Succeeded)
                    {
                        foreach (var err in roleAssign.Errors)
                        {
                            logger.LogError("Admin role assignment failed: {Error}", err.Description);
                        }
                    }
                }
                else
                {
                    foreach (var err in createResult.Errors)
                    {
                        logger.LogError("Admin creation failed: {Error}", err.Description);
                    }
                }
            }
        }
    }
}