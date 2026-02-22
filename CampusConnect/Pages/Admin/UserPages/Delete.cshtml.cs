using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace CampusConnect.Pages.UserPages
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DeleteModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public user user { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var u = await _context.users.FirstOrDefaultAsync(m => m.userID == id);

            if (u == null)
            {
                return NotFound();
            }
            user = u;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var u = await _context.users.FindAsync(id);
            if (u != null)
            {
                // delete linked Identity user if present
                if (!string.IsNullOrWhiteSpace(u.identityUserId))
                {
                    var identityUser = await _userManager.FindByIdAsync(u.identityUserId);
                    if (identityUser != null)
                    {
                        await _userManager.DeleteAsync(identityUser);
                    }
                }

                _context.users.Remove(u);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
