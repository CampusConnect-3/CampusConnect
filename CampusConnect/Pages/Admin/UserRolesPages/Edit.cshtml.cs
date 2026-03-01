using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserRolesPages
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly TablesDbContext _context;

        public EditModel(TablesDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public userRoles userRoles { get; set; } = default!;

        [BindProperty]
        public int roleID { get; set; }

        [BindProperty]
        public int userID { get; set; }

        public async Task<IActionResult> OnGetAsync(int? roleID, int? userID)
        {
            if (roleID == null || userID == null)
            {
                return NotFound();
            }

            var userroles = await _context.userRoles
                .FirstOrDefaultAsync(m => m.roleID == roleID && m.userID == userID);
            
            if (userroles == null)
            {
                return NotFound();
            }
            
            userRoles = userroles;
            this.roleID = roleID.Value;
            this.userID = userID.Value;

            ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
            ViewData["userID"] = new SelectList(_context.users, "userID", "email");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
                ViewData["userID"] = new SelectList(_context.users, "userID", "email");
                return Page();
            }

            var existing = await _context.userRoles
                .FirstOrDefaultAsync(m => m.roleID == roleID && m.userID == userID);

            if (existing == null)
            {
                return NotFound();
            }

            existing.roleID = userRoles.roleID;
            existing.userID = userRoles.userID;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!userRolesExists(roleID, userID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool userRolesExists(int roleId, int userId)
        {
            return _context.userRoles.Any(e => e.roleID == roleId && e.userID == userId);
        }
    }
}