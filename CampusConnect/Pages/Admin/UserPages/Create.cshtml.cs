using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CampusConnect.Pages.UserPages
{
    [Authorize(Roles="Admin")]
    public class CreateModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public user user { get; set; } = default!;

        // optional initial password for Identity account
        [BindProperty]
        public string? Password { get; set; }

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Create Identity user first (so we can link identityUserId)
            var identityUser = new IdentityUser { UserName = user.email, Email = user.email };

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

            // Do not store cleartext password in the app table
            // user.password = null;
            user.identityUserId = identityUser.Id;
            // Ensure username/email align
            user.username = identityUser.UserName ?? user.email;
            user.email = identityUser.Email ?? user.email;

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