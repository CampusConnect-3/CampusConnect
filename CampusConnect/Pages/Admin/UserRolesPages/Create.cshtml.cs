using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


namespace CampusConnect.Pages.UserRolesPages
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        private readonly ILogger<CreateModel> _logger;
        public CreateModel(TablesDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
        ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
        ViewData["userID"] = new SelectList(_context.users, "userID", "email");
            return Page();
        }

        [BindProperty]
        public userRoles userRoles { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.userRoles.Add(userRoles);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "CRITICAL: Role assigned. TargetUserId={TargetUserId} RoleId={RoleId} AdminUserId={AdminUserId} TraceId={TraceId}",
                userRoles.userID,
                userRoles.roleID,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                HttpContext.TraceIdentifier
            );

            return RedirectToPage("./Index");
        }
    }
}
