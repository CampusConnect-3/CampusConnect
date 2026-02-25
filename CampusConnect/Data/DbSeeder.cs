using CampusConnect.Constants;
<<<<<<< HEAD
using CampusConnect.Models; // <-- ApplicationUser
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
=======
using CampusConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
>>>>>>> origin/testing

namespace CampusConnect.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
<<<<<<< HEAD
            // Resolve managers (must match Program.cs Identity types)
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILoggerFactory>()
                                 .CreateLogger("DbSeeder");

            // -----------------------------
            // Seed roles safely
            // -----------------------------
            string[] roles =
=======
            //Seed Roles
            var userManager = service.GetService<UserManager<IdentityUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
            await roleManager.CreateAsync(new IdentityRole(Roles.User.ToString()));
            await roleManager.CreateAsync(new IdentityRole(Roles.Staff.ToString()));
            await roleManager.CreateAsync(new IdentityRole(Roles.Manager.ToString()));
            await roleManager.CreateAsync(new IdentityRole(Roles.Pending.ToString()));

            //Create Admin Identity user
            var adminIdentity = new IdentityUser
>>>>>>> origin/testing
            {
                Roles.Admin.ToString(),
                Roles.User.ToString(),
                Roles.Staff.ToString(),
                Roles.Manager.ToString()
            };

<<<<<<< HEAD
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
=======
            var userInDb = await userManager.FindByEmailAsync(adminIdentity.Email);
            if (userInDb == null)
            {
                await userManager.CreateAsync(adminIdentity, "Admin@123");
                await userManager.AddToRoleAsync(adminIdentity, Roles.Admin.ToString());
            }
            else
            {
                // ensure we have the Identity object to link
                adminIdentity = userInDb;
                
                // Ensure admin has Admin role
                if (!await userManager.IsInRoleAsync(adminIdentity, Roles.Admin.ToString()))
                {
                    await userManager.AddToRoleAsync(adminIdentity, Roles.Admin.ToString());
                }
            }

            // Create corresponding application user row and link to Identity user
            var tablesDb = service.GetService<TablesDbContext>();
            if (tablesDb != null)
            {
                // avoid duplicates by email or identityUserId
                var existingAppUser = await tablesDb.users
                    .FirstOrDefaultAsync(u => u.email == adminIdentity.Email || u.identityUserId == adminIdentity.Id);

                if (existingAppUser == null)
                {
                    var adminUser = new user
                    {
                        identityUserId = adminIdentity.Id,  // link them
                        fName = "System",
                        lName = "Admin",
                        username = adminIdentity.UserName ?? "admin",
                        email = adminIdentity.Email ?? "admin@example.com",
                        status = "Active",
                        password = null // do NOT store plaintext password
                    };

                    tablesDb.users.Add(adminUser);
                    await tablesDb.SaveChangesAsync();
>>>>>>> origin/testing
                }
            }
        }
    }
}