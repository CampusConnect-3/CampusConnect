using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class EditModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        private readonly ILogger<EditModel> _logger;
        public EditModel(TablesDbContext context, ILogger<EditModel> logger)
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

            var userroles =  await _context.userRoles.FirstOrDefaultAsync(m => m.roleID == id);
            if (userroles == null)
            {
                return NotFound();
            }
            userRoles = userroles;
           ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
           ViewData["userID"] = new SelectList(_context.users, "userID", "email");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(userRoles).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "CRITICAL: Role assignment edited. TargetUserId={TargetUserId} RoleId={RoleId} AdminUserId={AdminUserId} TraceId={TraceId}",
                    userRoles.userID,
                    userRoles.roleID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict editing user role. RoleId={RoleId} TargetUserId={TargetUserId} AdminUserId={AdminUserId} TraceId={TraceId}",
                    userRoles.roleID,
                    userRoles.userID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );

                if (!userRolesExists(userRoles.roleID))
                    return NotFound();

                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool userRolesExists(int id)
        {
            return _context.userRoles.Any(e => e.roleID == id);
        }
    }
}
