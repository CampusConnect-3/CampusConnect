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
    [Authorize(Roles="Admin")]
    public class IndexModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(CampusConnect.Data.TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<user> user { get; set; } = default!;
        
        // Add Users property for the view
        public IList<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        public async Task OnGetAsync()
        {
            user = await _context.users.ToListAsync();
            
            // Map to view model with Identity data
            Users = new List<UserViewModel>();
            foreach (var u in user)
            {
                var identityUser = !string.IsNullOrEmpty(u.identityUserId)
                    ? await _userManager.FindByIdAsync(u.identityUserId)
                    : null;

                Users.Add(new UserViewModel
                {
                    UserID = u.userID,
                    AppUserId = u.userID,  // Now nullable
                    FirstName = u.fName,
                    LastName = u.lName,
                    Email = u.email,
                    UserName = identityUser?.UserName ?? u.email,
                    AccountType = u.department ?? "Unknown",  // Use department as AccountType
                    IsApproved = u.status?.ToLowerInvariant().Contains("active") ?? false,  // Derive from status
                    Status = u.status ?? "Unknown",
                    AspNetUserId = u.identityUserId,
                    IdentityUserId = u.identityUserId
                });
            }
        }

        // View model for the user list
        public class UserViewModel
        {
            public int UserID { get; set; }
            public int? AppUserId { get; set; }  // Made nullable
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string Email { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string AccountType { get; set; } = string.Empty;
            public bool IsApproved { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? AspNetUserId { get; set; }
            public string? IdentityUserId { get; set; }
        }
    }
}