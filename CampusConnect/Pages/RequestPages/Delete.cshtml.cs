using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "Admin,Manager")]
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
        public Request request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var found = await _context.request.FirstOrDefaultAsync(m => m.requestID == id);
            if (found == null)
                return NotFound();

            request = found; // assign to PageModel property for Razor
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var found = await _context.request.FindAsync(id);
            if (found != null)
            {
                _context.request.Remove(found);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "CRITICAL: Request deleted. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    found.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }

            return RedirectToPage("./Index");
        }
    }
}