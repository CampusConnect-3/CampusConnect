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

        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
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

            OpenCount = allRequests.Count(r =>
                r.status != null &&
                r.status.statusName == RequestStatuses.ToDo);

            InProgressCount = allRequests.Count(r =>
                r.status != null &&
                r.status.statusName == RequestStatuses.InProgress);

            ClosedCount = allRequests.Count(r =>
                r.status != null &&
                r.status.statusName == RequestStatuses.Closed);

            RecentRequests = allRequests
                .Take(5)
                .ToList();
        }
    }
}