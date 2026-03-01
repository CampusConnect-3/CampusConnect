using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserPages
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public user user { get; set; } = default!;

        // Optional: admin can reset Identity password for linked user
        [BindProperty]
        public string? NewPassword { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var dbUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.userID == id);

            if (dbUser == null)
                return NotFound();

            user = dbUser;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Load from DB to prevent overposting
            var dbUser = await _context.users.FirstOrDefaultAsync(u => u.userID == user.userID);
            if (dbUser == null)
                return NotFound();

            // Update ONLY safe fields (do not touch identityUserId/status here)
            dbUser.fName = user.fName;
            dbUser.lName = user.lName;
            dbUser.username = user.username;
            dbUser.email = user.email;
            dbUser.department = user.department;

            // Never store a plaintext password in app table
            dbUser.password = null;

            await _context.SaveChangesAsync();

            // If admin provided a new password, reset Identity password for linked identity user
            if (!string.IsNullOrWhiteSpace(NewPassword) && !string.IsNullOrWhiteSpace(dbUser.identityUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(dbUser.identityUserId);
                if (identityUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Linked identity account not found.");
                    return Page();
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                var resetResult = await _userManager.ResetPasswordAsync(identityUser, token, NewPassword);

                if (!resetResult.Succeeded)
                {
                    foreach (var err in resetResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);

                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }
    }
}