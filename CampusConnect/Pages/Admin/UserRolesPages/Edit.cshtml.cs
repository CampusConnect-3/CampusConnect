using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserRolesPages
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(TablesDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public userRoles userRoles { get; set; } = default!;

        // Expose individual IDs for form binding
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

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int? roleID, int? userID)
        {
            if (roleID == null || userID == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
                ViewData["userID"] = new SelectList(_context.users, "userID", "email");
                return Page();
            }

            // Load existing record
            var existing = await _context.userRoles
                .FirstOrDefaultAsync(m => m.roleID == roleID && m.userID == userID);

            if (existing == null)
            {
                return NotFound();
            }

            // Update properties
            existing.roleID = userRoles.roleID;
            existing.userID = userRoles.userID;

            try
            {
                // Join table edit = remove old mapping + add new mapping
                _context.userRoles.Remove(existing);
                _context.userRoles.Add(new userRoles
                {
                    roleID = userRoles.roleID,
                    userID = userRoles.userID
                });

                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "CRITICAL: Role assignment edited. OldRoleId={OldRoleId} OldUserId={OldUserId} NewRoleId={NewRoleId} NewUserId={NewUserId} AdminUserId={AdminUserId} TraceId={TraceId}",
                    roleID,
                    userID,
                    userRoles.roleID,
                    userRoles.userID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict editing user role. OldRoleId={OldRoleId} OldUserId={OldUserId} AdminUserId={AdminUserId} TraceId={TraceId}",
                    roleID,
                    userID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool userRolesExists(int roleId, int userId)
        {
            return _context.userRoles.Any(e => e.roleID == roleId && e.userID == userId);
        }
    }
}