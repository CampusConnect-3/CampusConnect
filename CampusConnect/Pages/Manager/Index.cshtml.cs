using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        private readonly TablesDbContext _context;

        public IndexModel(TablesDbContext context)
        {
            _context = context;
        }

        public int UnassignedCount { get; set; }
        public int InProgressCount { get; set; }
        public int HighPriorityCount { get; set; }
        public int ClosedCount { get; set; }

        public List<request> RecentRequests { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            var allRequests = await _context.request
                .AsNoTracking()
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .OrderByDescending(r => r.createdAt)
                .ToListAsync(cancellationToken);

            UnassignedCount = allRequests.Count(r =>
                r.assigned_to == null &&
                r.status != null &&
                r.status.statusName != RequestStatuses.Closed);

            InProgressCount = allRequests.Count(r =>
                r.status != null &&
                r.status.statusName == RequestStatuses.InProgress);

            HighPriorityCount = allRequests.Count(r =>
                (r.priority == "High" || r.priority == "Critical") &&
                r.status != null &&
                r.status.statusName != RequestStatuses.Closed);

            ClosedCount = allRequests.Count(r =>
                r.status != null &&
                r.status.statusName == RequestStatuses.Closed);

            RecentRequests = allRequests
                .Where(r => r.status == null || r.status.statusName != RequestStatuses.Closed)
                .Take(5)
                .ToList();
        }
    }
}