using RoleConstants = CampusConnect.Constants.Roles;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public ManageRolesModel(TablesDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public User? AppUser { get; set; }
        public ApplicationUser? IdentityUser { get; set; }
        public IList<string> CurrentRoles { get; set; } = new List<string>();
        public List<string> AllRoles { get; set; } = new List<string>();

        [BindProperty]
        public List<string> SelectedRoles { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            AppUser = await _context.users.FirstOrDefaultAsync(u => u.userID == id);
            if (AppUser == null || string.IsNullOrWhiteSpace(AppUser.IdentityUserId))
                return NotFound();

            IdentityUser = await _userManager.FindByIdAsync(AppUser.IdentityUserId);
            if (IdentityUser == null)
                return NotFound();

            CurrentRoles = await _userManager.GetRolesAsync(IdentityUser);

            AllRoles = new List<string>
            {
                RoleConstants.Admin.ToString(),
                RoleConstants.Manager.ToString(),
                RoleConstants.Staff.ToString(),
                RoleConstants.User.ToString(),
                RoleConstants.Pending.ToString()
            };

            // Pre-select current roles (helps the UI)
            SelectedRoles = CurrentRoles.ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
                return NotFound();

            AppUser = await _context.users.FirstOrDefaultAsync(u => u.userID == id);
            if (AppUser == null || string.IsNullOrWhiteSpace(AppUser.IdentityUserId))
                return NotFound();

            IdentityUser = await _userManager.FindByIdAsync(AppUser.IdentityUserId);
            if (IdentityUser == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(IdentityUser);

            var selected = (SelectedRoles ?? new List<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct()
                .ToList();

            var removeResult = await _userManager.RemoveFromRolesAsync(IdentityUser, currentRoles);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Failed to remove existing roles.");
                return Page();
            }

            if (selected.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(IdentityUser, selected);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to add selected roles.");
                    return Page();
                }

                AppUser.status = selected.Contains(RoleConstants.Pending.ToString()) && selected.Count == 1
                    ? "Pending"
                    : "Active";
            }
            else
            {
                await _userManager.AddToRoleAsync(IdentityUser, RoleConstants.Pending.ToString());
                AppUser.status = "Pending";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Roles updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}