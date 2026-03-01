using CampusConnect.Data;
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
    public class CreateModel : PageModel
    {
        private readonly TablesDbContext _context;
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
        public userRoles userRoles { get; set; } = new userRoles();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
                ViewData["userID"] = new SelectList(_context.users, "userID", "email");
                return Page();
            }

            // Prevent duplicates (composite PK roleID + userID)
            var exists = await _context.userRoles.AnyAsync(ur =>
                ur.userID == userRoles.userID && ur.roleID == userRoles.roleID);

            if (exists)
            {
                ModelState.AddModelError(string.Empty, "That user already has this role.");
                ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
                ViewData["userID"] = new SelectList(_context.users, "userID", "email");
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