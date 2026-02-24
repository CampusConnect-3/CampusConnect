using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CampusConnect.Pages.UserRolesPages
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        private readonly ILogger<DeleteModel> _logger;
        public DeleteModel(TablesDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public userRoles userRoles { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userroles = await _context.userRoles.FirstOrDefaultAsync(m => m.roleID == id);

            if (userroles == null)
            {
                return NotFound();
            }
            else
            {
                userRoles = userroles;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userroles = await _context.userRoles.FindAsync(id);
            if (userroles != null)
            {
                userRoles = userroles;
                _context.userRoles.Remove(userRoles);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "CRITICAL: Role removed. TargetUserId={TargetUserId} RoleId={RoleId} AdminUserId={AdminUserId} TraceId={TraceId}",
                    userRoles.userID,
                    userRoles.roleID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }

            return RedirectToPage("./Index");
        }
    }
}
