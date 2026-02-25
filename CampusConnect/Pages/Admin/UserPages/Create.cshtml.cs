using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CampusConnect.Pages.UserPages
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(TablesDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public User user { get; set; } = default!;

        // Optional initial password for Identity account
        [BindProperty]
        public string? Password { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Create Identity user first (so we can link IdentityUserId)
            var identityUser = new ApplicationUser
            {
                UserName = user.email,
                Email = user.email,
                EmailConfirmed = true
            };

            IdentityResult createResult;
            if (!string.IsNullOrWhiteSpace(Password))
            {
                createResult = await _userManager.CreateAsync(identityUser, Password);
            }
            else
            {
                // create without password - external or set later
                createResult = await _userManager.CreateAsync(identityUser);
            }

            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                return Page();
            }

            // Link legacy profile to Identity user (PascalCase property)
            user.IdentityUserId = identityUser.Id;

            // Ensure username/email align (optional)
            user.username = identityUser.UserName ?? user.email ?? "";
            user.email = identityUser.Email ?? user.email;

            // Do not store plaintext password
            user.password = "IDENTITY_ONLY"; // or null if your DB allows nulls

            try
            {
                _context.users.Add(user);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // rollback Identity user to avoid orphan
                await _userManager.DeleteAsync(identityUser);
                ModelState.AddModelError(string.Empty, "Unable to create profile. Please try again or contact an administrator.");
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}