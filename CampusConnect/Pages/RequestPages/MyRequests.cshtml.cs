using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "User")]
    public class MyRequestsModel : PageModel
    {
        private readonly TablesDbContext _context;

        public MyRequestsModel(TablesDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string View { get; set; } = "active";

        public IList<RequestListItem> Requests { get; set; } = new List<RequestListItem>();

        public SelectList? StatusOptions { get; set; }
        public bool ShowingHistory => string.Equals(View, "history", StringComparison.OrdinalIgnoreCase);

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            // Populate status dropdown
            var statuses = await _context.requestStatus
                .OrderBy(s => s.statusName)
                .ToListAsync(cancellationToken);

            StatusOptions = new SelectList(statuses, "statusID", "statusName");

            // Resolve current user by email (Identity uses email as username)
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
            {
                Requests = new List<RequestListItem>();
                return Page();
            }

            var appUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId, cancellationToken);

            if (appUser == null)
            {
                Requests = new List<RequestListItem>();
                return Page();
            }

            // Query requests created by this user, include navigation properties
            var q = _context.request
                .AsNoTracking()
                .Include(r => r.status)
                .Include(r => r.category)
                .Where(r => r.created_by == appUser.userID)
                .AsQueryable();

            if (ShowingHistory)
            {
                q = q.Where(r => r.status != null && r.status.statusName == RequestStatuses.Closed);
            }
            else
            {
                q = q.Where(r => r.status == null || r.status.statusName != RequestStatuses.Closed);
            }

            if (!string.IsNullOrEmpty(StatusFilter))
            {
                if (int.TryParse(StatusFilter, out var sid))
                {
                    q = q.Where(r => r.statusID == sid);
                }
                else
                {
                    q = q.Where(r => r.status != null && r.status.statusName == StatusFilter);
                }
            }

            q = q.OrderByDescending(r => r.createdAt);

            var requestRows = await q.ToListAsync(cancellationToken);

            var requestIds = requestRows.Select(r => r.requestID).ToList();
            var commentStats = await _context.requestComments
                .AsNoTracking()
                .Where(c => requestIds.Contains(c.requestID))
                .GroupBy(c => c.requestID)
                .Select(g => new
                {
                    RequestId = g.Key,
                    Count = g.Count(),
                    LastCommentAt = g.Max(c => c.createdAt)
                })
                .ToDictionaryAsync(
                    x => x.RequestId,
                    x => new { x.Count, x.LastCommentAt },
                    cancellationToken);

            Requests = requestRows
                .Select(r =>
                {
                    commentStats.TryGetValue(r.requestID, out var stats);

                    return new RequestListItem
                    {
                        Request = r,
                        CommentCount = stats?.Count ?? 0,
                        LastActivityAt = stats?.LastCommentAt ?? r.createdAt
                    };
                })
                .ToList();

            return Page();
        }

        public class RequestListItem
        {
            public request Request { get; set; } = default!;
            public int CommentCount { get; set; }
            public DateTime LastActivityAt { get; set; }
        }
    }
}
