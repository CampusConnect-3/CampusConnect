using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "Admin,Manager,Staff,User")]
    public class DetailsModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(TablesDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public request request { get; private set; } = default!;
        public List<attachments> Attachments { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            try
            {
                var req = await _context.request
                    .AsNoTracking()
                    .Include(r => r.createdBy)
                    .Include(r => r.assignedTo)
                    .Include(r => r.status)
                    .Include(r => r.category)
                    .FirstOrDefaultAsync(m => m.requestID == id, cancellationToken);

                if (req == null)
                {
                    _logger.LogWarning("DETAILS NOT FOUND. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                        id,
                        User.FindFirstValue(ClaimTypes.NameIdentifier),
                        HttpContext.TraceIdentifier);
                    return NotFound();
                }

                request = req;

                // Load attachments for this request
                Attachments = await _context.attachments
                    .AsNoTracking()
                    .Where(a => a.requestID == id)
                    .Include(a => a.creator)
                    .OrderByDescending(a => a.uploadedAt)
                    .ToListAsync(cancellationToken);

                // Log as Information (view access)
                _logger.LogInformation("DETAILS VIEWED. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    HttpContext.TraceIdentifier);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DETAILS GET FAILED. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    HttpContext.TraceIdentifier);
                throw; // global exception handler => /Error
            }
        }
    }
}