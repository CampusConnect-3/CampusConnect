using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CampusConnect.Pages.Admin.UserPages
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public user user { get; set; } = default!;
        public string RoleName { get; set; } = "No Role";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var u = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.userID == id);

            if (u == null)
                return NotFound();

            user = u;

            if (!string.IsNullOrEmpty(user.identityUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(user.identityUserId);
                if (identityUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(identityUser);
                    RoleName = roles.Any() ? string.Join(", ", roles) : "No Role";
                }
            }

            return Page();
        }
    }
}