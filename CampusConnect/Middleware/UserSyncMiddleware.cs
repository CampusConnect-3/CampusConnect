using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusConnect.Middleware
{
    public class UserSyncMiddleware
    {
        private readonly RequestDelegate _next;

        public UserSyncMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TablesDbContext tablesDb, UserManager<IdentityUser> userManager)
        {
            // Only run for authenticated users
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var identityUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(identityUserId))
                {
                    // Check if app profile exists
                    var appUserExists = await tablesDb.users
                        .AnyAsync(u => u.identityUserId == identityUserId);

                    if (!appUserExists)
                    {
                        // Get Identity user details
                        var identityUser = await userManager.FindByIdAsync(identityUserId);

                        if (identityUser != null)
                        {
                            // Auto-create app profile
                            var appUser = new user
                            {
                                identityUserId = identityUserId,
                                email = identityUser.Email ?? "unknown@example.com",
                                username = identityUser.UserName ?? identityUser.Email ?? "unknown",
                                fName = "Pending",
                                lName = "Setup",
                                department = null,
                                status = "Pending"
                            };

                            tablesDb.users.Add(appUser);
                            await tablesDb.SaveChangesAsync();
                        }
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method for easy registration
    public static class UserSyncMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserSync(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserSyncMiddleware>();
        }
    }
}
