using CampusConnect.Models; // ApplicationUser + legacy User entity
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// ⭐ Alias to prevent ambiguity with Models.roles entity
using RoleConstants = CampusConnect.Constants.Roles;

namespace CampusConnect.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            // Resolve services (must match Program.cs Identity types)
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
            var tablesDb = services.GetRequiredService<TablesDbContext>();

            // -------------------------------------
            // Seed roles safely
            // -------------------------------------
            string[] roles =
            {
                RoleConstants.Admin.ToString(),
                RoleConstants.User.ToString(),
                RoleConstants.Staff.ToString(),
                RoleConstants.Manager.ToString(),
                RoleConstants.Pending.ToString()
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
                            logger.LogError("Role creation failed for {Role}: {Error}", role, err.Description);
                        }
                    }
                }
            }

            // -------------------------------------
            // Seed admin Identity user
            // -------------------------------------
            const string adminEmail = "admin@gmail.com";
            const string adminPassword = "Admin@123";

            var adminInDb = await userManager.FindByEmailAsync(adminEmail);

            if (adminInDb == null)
            {
                adminInDb = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminInDb, adminPassword);

                if (!createResult.Succeeded)
                {
                    foreach (var err in createResult.Errors)
                        logger.LogError("Admin creation failed: {Error}", err.Description);

                    return;
                }
            }

            var adminIdentityId = adminInDb.Id;
            var adminIdentityEmail = adminInDb.Email ?? adminEmail;
            var adminIdentityUserName = adminInDb.UserName ?? adminEmail;

            // Ensure Admin role is assigned
            if (!await userManager.IsInRoleAsync(adminInDb, RoleConstants.Admin.ToString()))
            {
                var roleAssign = await userManager.AddToRoleAsync(adminInDb, RoleConstants.Admin.ToString());

                if (!roleAssign.Succeeded)
                {
                    foreach (var err in roleAssign.Errors)
                        logger.LogError("Admin role assignment failed: {Error}", err.Description);
                }
            }

            // -------------------------------------
            // Seed legacy profile row
            // -------------------------------------
            var existingLegacy = await tablesDb.users.FirstOrDefaultAsync(u =>
                u.email == adminIdentityEmail || u.IdentityUserId == adminIdentityId);

            if (existingLegacy == null)
            {
                var legacyAdmin = new User
                {
                    IdentityUserId = adminIdentityId,
                    fName = "System",
                    lName = "Admin",
                    username = adminIdentityUserName,
                    email = adminIdentityEmail,
                    status = "Active",
                    password = "IDENTITY_ONLY"
                };

                tablesDb.users.Add(legacyAdmin);
                await tablesDb.SaveChangesAsync();
            }
        }
    }
}