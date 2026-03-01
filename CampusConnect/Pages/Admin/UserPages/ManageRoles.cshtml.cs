using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
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

        // Display-only
        public user? AppUser { get; set; }
        public IdentityUser? IdentityUser { get; set; }
        public IList<string> CurrentRoles { get; set; } = new List<string>();
        public List<string> AllRoles { get; set; } = new List<string>();

        // Route/query parameter (Identity user id)
        [BindProperty(SupportsGet = true)]
        public string? IdentityUserId { get; set; }

        [BindProperty]
        public List<string> SelectedRoles { get; set; } = new List<string>();

        private async Task<bool> LoadPageAsync(string? identityUserId)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
                return false;

            IdentityUser = await _userManager.FindByIdAsync(identityUserId);
            if (IdentityUser == null)
                return false;

            // Try to load existing app profile
            AppUser = await _context.users.FirstOrDefaultAsync(u => u.identityUserId == identityUserId);

            // If missing, auto-create a minimal app profile so Admin can manage it
            if (AppUser == null)
            {
                AppUser = new user
                {
                    identityUserId = identityUserId,
                    email = IdentityUser.Email ?? "",
                    username = IdentityUser.UserName ?? IdentityUser.Email ?? "",
                    fName = "N/A",
                    lName = "N/A",
                    department = null,
                    status = "Pending"
                };

                _context.users.Add(AppUser);
                await _context.SaveChangesAsync();
            }

            CurrentRoles = await _userManager.GetRolesAsync(IdentityUser);

            AllRoles = new List<string>
            {
                Roles.Admin.ToString(),
                Roles.Manager.ToString(),
                Roles.Staff.ToString(),
                Roles.User.ToString(),
                Roles.Pending.ToString()
            };

            return true;
        }

        public async Task<IActionResult> OnGetAsync(string? identityUserId)
        {
            IdentityUserId = identityUserId;

            var ok = await LoadPageAsync(IdentityUserId);
            if (!ok) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? identityUserId)
        {
            IdentityUserId = identityUserId;

            var ok = await LoadPageAsync(IdentityUserId);
            if (!ok) return NotFound();

            var identityUser = IdentityUser!;
            var appUser = AppUser!;

            var currentRoles = await _userManager.GetRolesAsync(identityUser);

            // Remove current roles
            var removeResult = await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Failed to remove existing roles.");
                await LoadPageAsync(IdentityUserId);
                return Page();
            }

            // If nothing selected, default to Pending
            var rolesToAdd = (SelectedRoles != null && SelectedRoles.Any())
                ? SelectedRoles
                : new List<string> { Roles.Pending.ToString() };

            var addResult = await _userManager.AddToRolesAsync(identityUser, rolesToAdd);
            if (!addResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Failed to add selected roles.");
                await LoadPageAsync(IdentityUserId);
                return Page();
            }

            // Update app user status based on roles
            appUser.status = rolesToAdd.Contains(Roles.Pending.ToString()) ? "Pending" : "Active";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Roles updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}