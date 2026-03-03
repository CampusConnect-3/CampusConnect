using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
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
        public request request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            try
            {
                var req = await _context.request
                    .AsNoTracking()
                    .Include(r => r.category)
                    .Include(r => r.status)
                    .FirstOrDefaultAsync(m => m.requestID == id, cancellationToken);

                if (req == null)
                {
                    _logger.LogWarning("DELETE GET NOT FOUND. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                        id,
                        User.FindFirstValue(ClaimTypes.NameIdentifier),
                        HttpContext.TraceIdentifier);
                    return NotFound();
                }

                request = req;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE GET FAILED. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    HttpContext.TraceIdentifier);
                throw; // let global exception handler render /Error
            }
        }

        public async Task<IActionResult> OnPostAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            try
            {
                var req = await _context.request
                    .FirstOrDefaultAsync(r => r.requestID == id, cancellationToken);

                if (req == null)
                {
                    _logger.LogWarning("DELETE POST NOT FOUND. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                        id,
                        User.FindFirstValue(ClaimTypes.NameIdentifier),
                        HttpContext.TraceIdentifier);
                    return NotFound();
                }

                _context.request.Remove(req);
                await _context.SaveChangesAsync(cancellationToken);

                // ✅ Critical operation log
                _logger.LogWarning("CRITICAL OPERATION: REQUEST DELETED. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    HttpContext.TraceIdentifier);

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE POST FAILED. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    HttpContext.TraceIdentifier);
                throw; // let global exception handler render /Error
            }
        }
    }
}