using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.UserPages
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(TablesDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public User user { get; set; } = default!;

        // Optional new password to set via Identity (admin action)
        [BindProperty]
        public string? NewPassword { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var u = await _context.users.FirstOrDefaultAsync(m => m.userID == id);

            if (u == null)
                return NotFound();

            user = u;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Ensure we do not save plaintext password in legacy table
            user.password = "IDENTITY_ONLY";

            _context.Attach(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(user.userID))
                    return NotFound();
                else
                    throw;
            }

            // If admin provided a new password, reset Identity password
            if (!string.IsNullOrWhiteSpace(NewPassword) &&
                !string.IsNullOrWhiteSpace(user.IdentityUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);

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

        private bool UserExists(int id)
        {
            return _context.users.Any(e => e.userID == id);
        }
    }
}