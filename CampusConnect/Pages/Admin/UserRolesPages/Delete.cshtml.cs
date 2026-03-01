using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserRolesPages
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(TablesDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public userRoles userRoles { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? roleID, int? userID)
        {
            if (roleID == null || userID == null)
                return NotFound();

            var ur = await _context.userRoles
                .AsNoTracking()
                .Include(x => x.role)
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.roleID == roleID && x.userID == userID);

            if (ur == null)
                return NotFound();

            userRoles = ur;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // values come from hidden fields in the form
            var roleID = userRoles.roleID;
            var userID = userRoles.userID;

            var ur = await _context.userRoles
                .FirstOrDefaultAsync(x => x.roleID == roleID && x.userID == userID);

            if (ur == null)
            {
                // already removed or invalid
                return RedirectToPage("./Index");
            }

            _context.userRoles.Remove(ur);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "CRITICAL: Role removed. TargetUserId={TargetUserId} RoleId={RoleId} AdminUserId={AdminUserId} TraceId={TraceId}",
                ur.userID,
                ur.roleID,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                HttpContext.TraceIdentifier
            );

            return RedirectToPage("./Index");
        }
    }
}