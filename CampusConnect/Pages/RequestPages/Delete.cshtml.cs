using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
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
    [Authorize(Roles = "Admin,Manager")]
    public class DeleteModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<DeleteModel> _logger;
        private readonly IActivityLoggerService _activityLogger;

        public DeleteModel(
            TablesDbContext context, 
            ILogger<DeleteModel> logger,
            IActivityLoggerService activityLogger)
        {
            _context = context;
            _logger = logger;
            _activityLogger = activityLogger;
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
                    _logger.LogWarning("DELETE GET NOT FOUND. RequestId={RequestId}", id);
                    return NotFound();
                }

                request = req;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE GET FAILED. RequestId={RequestId}", id);
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            try
            {
                var req = await _context.request
                    .Include(r => r.category)
                    .FirstOrDefaultAsync(r => r.requestID == id, cancellationToken);

                if (req == null)
                {
                    _logger.LogWarning("DELETE POST NOT FOUND. RequestId={RequestId}", id);
                    return NotFound();
                }

                // LOG THE DELETION BEFORE REMOVING
                await _activityLogger.LogActivityAsync(
                    action: "deleted_request",
                    requestId: req.requestID,
                    requestTitle: req.title,
                    details: new Dictionary<string, object>
                    {
                        { "category", req.category?.categoryName ?? "Unknown" },
                        { "priority", req.priority },
                        { "building", req.buildingName },
                        { "room", req.roomNumber }
                    }
                );

                _context.request.Remove(req);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("CRITICAL: REQUEST DELETED. RequestId={RequestId} UserId={UserId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE POST FAILED. RequestId={RequestId}", id);
                throw;
            }
        }
    }
}