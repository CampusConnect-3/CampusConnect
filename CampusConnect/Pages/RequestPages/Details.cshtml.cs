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
        public int CommentCount { get; private set; }

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

                if (!await CanAccessRequestAsync(req, cancellationToken))
                    return Forbid();

                request = req;
                await LoadRelatedDataAsync(req.requestID, cancellationToken);

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

        private async Task LoadRelatedDataAsync(int requestId, CancellationToken cancellationToken)
        {
            Attachments = await _context.attachments
                .AsNoTracking()
                .Where(a => a.requestID == requestId)
                .Include(a => a.creator)
                .OrderByDescending(a => a.uploadedAt)
                .ToListAsync(cancellationToken);

            CommentCount = await _context.requestComments
                .AsNoTracking()
                .Where(c => c.requestID == requestId)
                .CountAsync(cancellationToken);
        }

        private async Task<bool> CanAccessRequestAsync(request req, CancellationToken cancellationToken)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff"))
                return true;

            if (!User.IsInRole("User"))
                return false;

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
                return false;

            var appUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId, cancellationToken);

            return appUser != null && req.created_by == appUser.userID;
        }

        public string GetDisplayName(user? person, string fallback = "Unknown user")
        {
            if (person == null)
                return fallback;

            var fullName = $"{person.fName} {person.lName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            if (!string.IsNullOrWhiteSpace(person.email))
                return person.email;

            return fallback;
        }
    }
}
