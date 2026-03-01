using CampusConnect.Constants;
using CampusConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace CampusConnect.Data
{
    public class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            //Seed Roles
            var userManager = service.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles =
            {
                Roles.Admin.ToString(),
                Roles.User.ToString(),
                Roles.Staff.ToString(),
                Roles.Manager.ToString(),
                Roles.Pending.ToString()
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            //Create Admin Identity user
            var adminIdentity = new IdentityUser
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

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
                }
            }
        }
    }       
}
