using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace CampusConnect.Pages.Admin.UserPages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<user> user { get; set; } = default!;
        public IList<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingUsers { get; set; }
        public int StaffAndManagers { get; set; }

        public async Task OnGetAsync()
        {
            user = await _context.users.ToListAsync();

            var mappedUsers = new List<UserViewModel>();

            foreach (var u in user)
            {
                IdentityUser? identityUser = null;
                IList<string> roles = new List<string>();

                if (!string.IsNullOrEmpty(u.identityUserId))
                {
                    identityUser = await _userManager.FindByIdAsync(u.identityUserId);

                    if (identityUser != null)
                    {
                        roles = await _userManager.GetRolesAsync(identityUser);
                    }
                }

                var primaryRole = roles.Any() ? string.Join(", ", roles) : "No Role";

                mappedUsers.Add(new UserViewModel
                {
                    UserID = u.userID,
                    AppUserId = u.userID,
                    FirstName = u.fName,
                    LastName = u.lName,
                    FullName = $"{u.fName} {u.lName}".Trim(),
                    Email = u.email,
                    UserName = identityUser?.UserName ?? u.username ?? u.email,
                    RoleName = primaryRole,
                    IsApproved = u.status?.ToLowerInvariant().Contains("active") ?? false,
                    Status = u.status ?? "Unknown",
                    AspNetUserId = u.identityUserId,
                    IdentityUserId = u.identityUserId
                });
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLowerInvariant();

                mappedUsers = mappedUsers
                    .Where(u =>
                        (!string.IsNullOrWhiteSpace(u.FullName) && u.FullName.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrWhiteSpace(u.UserName) && u.UserName.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrWhiteSpace(u.Email) && u.Email.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrWhiteSpace(u.RoleName) && u.RoleName.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrWhiteSpace(u.Status) && u.Status.ToLowerInvariant().Contains(term)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(RoleFilter))
            {
                mappedUsers = mappedUsers
                    .Where(u => !string.IsNullOrWhiteSpace(u.RoleName) &&
                                u.RoleName.Contains(RoleFilter))
                    .ToList();
            }

            Users = mappedUsers
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToList();

            TotalUsers = mappedUsers.Count;
            ActiveUsers = mappedUsers.Count(u => u.Status.ToLowerInvariant().Contains("active"));
            PendingUsers = mappedUsers.Count(u => u.Status.ToLowerInvariant().Contains("pending"));
            StaffAndManagers = mappedUsers.Count(u =>
                u.RoleName.Contains("Staff") || u.RoleName.Contains("Manager"));
        }

        public class UserViewModel
        {
            public int UserID { get; set; }
            public int? AppUserId { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string RoleName { get; set; } = string.Empty;
            public bool IsApproved { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? AspNetUserId { get; set; }
            public string? IdentityUserId { get; set; }
        }
    }
}