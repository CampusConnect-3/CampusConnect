using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserPages
{
    [Authorize(Roles = "Admin")]
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
                var identityUser = !string.IsNullOrEmpty(u.aspNetUserId)
                    ? await _userManager.FindByIdAsync(u.aspNetUserId)
                    : null;

                Users.Add(new UserViewModel
                {
                    UserID = u.userID,
                    FirstName = u.firstName,
                    LastName = u.lastName,
                    Email = u.email,
                    UserName = identityUser?.UserName ?? u.email,
                    AccountType = u.accountType ?? "Unknown",
                    IsApproved = u.isApproved,
                    AspNetUserId = u.aspNetUserId
                });
            }
        }

        // View model for the user list
        public class UserViewModel
        {
            public int UserID { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string Email { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string AccountType { get; set; } = string.Empty;
            public bool IsApproved { get; set; }
            public string? AspNetUserId { get; set; }
        }
    }
}