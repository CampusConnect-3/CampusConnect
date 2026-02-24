using CampusConnect.Constants;
using CampusConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserPages
{
    [Authorize(Roles = "Admin")]
    public class ManageRolesModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ManageRolesModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public CampusConnect.Models.user AppUser { get; set; }
        public IdentityUser IdentityUser { get; set; }
        public IList<string> CurrentRoles { get; set; }
        public List<string> AllRoles { get; set; }

        [BindProperty]
        public List<string> SelectedRoles { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AppUser = await _context.users.FirstOrDefaultAsync(u => u.userID == id);
            if (AppUser == null || string.IsNullOrEmpty(AppUser.identityUserId))
            {
                return NotFound();
            }

            IdentityUser = await _userManager.FindByIdAsync(AppUser.identityUserId);
            if (IdentityUser == null)
            {
                return NotFound();
            }

            CurrentRoles = await _userManager.GetRolesAsync(IdentityUser);
            AllRoles = new List<string>
            {
                Roles.Admin.ToString(),
                Roles.Manager.ToString(),
                Roles.Staff.ToString(),
                Roles.User.ToString()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AppUser = await _context.users.FirstOrDefaultAsync(u => u.userID == id);
            if (AppUser == null || string.IsNullOrEmpty(AppUser.identityUserId))
            {
                return NotFound();
            }

            IdentityUser = await _userManager.FindByIdAsync(AppUser.identityUserId);
            if (IdentityUser == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(IdentityUser);
            
            // Remove all current roles (except we'll handle later)
            var removeResult = await _userManager.RemoveFromRolesAsync(IdentityUser, currentRoles);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Failed to remove existing roles.");
                return Page();
            }

            // Add selected roles
            if (SelectedRoles != null && SelectedRoles.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(IdentityUser, SelectedRoles);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to add selected roles.");
                    return Page();
                }

                // Update user status to Active since they now have a role
                AppUser.status = "Active";
            }
            else
            {
                // No roles selected, assign Pending
                await _userManager.AddToRoleAsync(IdentityUser, Roles.Pending.ToString());
                AppUser.status = "Pending";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Roles updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}