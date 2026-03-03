using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages
{
    [Authorize(Roles = "Admin,Manager,Staff,User")]
    public class IndexModel : PageModel
    {
        private readonly TablesDbContext _context;

        public IndexModel(TablesDbContext context)
        {
            _context = context;
        }

        // Dashboard display info
        public string DisplayName { get; private set; } = "User";
        public string StudentId { get; private set; } = "N/A";

        public int OpenCount { get; private set; }
        public int InProgressCount { get; private set; }
        public int ClosedCount { get; private set; }

        // Used for "View All" logic
        public int TotalMyRequests { get; private set; }

        // Recent requests (your Index.cshtml expects List<request>)
        public List<request> RecentRequests { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            // Role-based landing page
            if (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff"))
                return RedirectToPage("/Admin/Dashboard");

            // Student/User dashboard logic
            DisplayName = User.Identity?.Name ?? "User";
            StudentId = User.FindFirst("student_id")?.Value ?? "N/A";

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(identityUserId))
                return Page();

            var appUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId, cancellationToken);

            // If user isn't linked to app profile yet, show dashboard but empty
            if (appUser == null)
                return Page();

            // Prefer app profile username as School ID (since that's how you store it)
            StudentId = string.IsNullOrWhiteSpace(appUser.username) ? StudentId : appUser.username;

            var myRequestsQuery = _context.request
                .AsNoTracking()
                .Include(r => r.status)
                .Where(r => r.created_by == appUser.userID);

            TotalMyRequests = await myRequestsQuery.CountAsync(cancellationToken);

            var allMyRequests = await myRequestsQuery.ToListAsync(cancellationToken);

            OpenCount = allMyRequests.Count(r => r.status?.statusName == RequestStatuses.ToDo);
            InProgressCount = allMyRequests.Count(r => r.status?.statusName == RequestStatuses.InProgress);
            ClosedCount = allMyRequests.Count(r => r.status?.statusName == RequestStatuses.Closed);

            RecentRequests = allMyRequests
                .OrderByDescending(r => r.createdAt)
                .Take(3)
                .ToList();

            return Page();
        }
    }
}