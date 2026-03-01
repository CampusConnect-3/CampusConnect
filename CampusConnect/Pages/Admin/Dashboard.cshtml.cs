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

namespace CampusConnect.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly TablesDbContext _context;

        public DashboardModel(TablesDbContext context)
        {
            _context = context;
        }

        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ClosedCount { get; set; }

        public List<request> RecentRequests { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            // Load all requests with their status/navigation once
            var allRequests = await _context.request
                .AsNoTracking()
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .ToListAsync(cancellationToken);

            // Calculate counts in-memory (single DB query)
            OpenCount = allRequests.Count(r => r.status?.statusName == RequestStatuses.ToDo);
            InProgressCount = allRequests.Count(r => r.status?.statusName == RequestStatuses.InProgress);
            ClosedCount = allRequests.Count(r => r.status?.statusName == RequestStatuses.Closed);

            // Get recent requests from the already-loaded collection
            RecentRequests = allRequests
                .OrderByDescending(r => r.createdAt)
                .Take(5)
                .ToList();
        }
    }
}