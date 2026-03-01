using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserPages
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
                return NotFound();

            var u = await _context.users.FirstOrDefaultAsync(x => x.userID == id);
            if (u == null)
                return RedirectToPage("./Index");

            // Check dependencies (because your FK rules are Restrict)
            var hasCreatedRequests = await _context.request.AnyAsync(r => r.created_by == u.userID);
            var hasComments = await _context.requestComments.AnyAsync(c => c.creatorID == u.userID);
            var hasAttachments = await _context.attachments.AnyAsync(a => a.creatorID == u.userID);

            if (hasCreatedRequests || hasComments || hasAttachments)
            {
                ModelState.AddModelError(string.Empty,
                    "Cannot delete this user because they are linked to requests/comments/attachments. " +
                    "Disable the account or remove dependencies first.");
                user = u; // keep the page populated
                return Page();
            }

            // Delete linked Identity user if present
            if (!string.IsNullOrWhiteSpace(u.identityUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(u.identityUserId);
                if (identityUser != null)
                {
                    var identityDelete = await _userManager.DeleteAsync(identityUser);
                    if (!identityDelete.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to delete linked Identity account.");
                        user = u;
                        return Page();
                    }
                }
            }

            _context.users.Remove(u);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}