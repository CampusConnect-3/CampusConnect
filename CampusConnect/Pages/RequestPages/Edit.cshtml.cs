using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "Admin,Manager")]
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
        public Request request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var found = await _context.request.FirstOrDefaultAsync(m => m.requestID == id);
            if (found == null)
                return NotFound();

            request = found;

            ViewData["assigned_to"] = new SelectList(_context.users, "userID", "email");
            ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
            ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
            ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            _context.Attach(request).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "CRITICAL: Request edited. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    request.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrency conflict editing request. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    request.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );

                if (!RequestExists(request.requestID))
                    return NotFound();

                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool RequestExists(int id)
        {
            return _context.request.Any(e => e.requestID == id);
        }
    }
}