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
        public string DisplayName { get; set; } = "";
        public string StudentId { get; set; } = "N/A";

        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ClosedCount { get; set; }

        // Used for "View All" logic
        public int TotalMyRequests { get; set; }

        // Recent requests
        public List<request> RecentRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            // Role-based landing page
            // Admin should never see the student dashboard
            if (User.IsInRole("Admin"))
                return RedirectToPage("/Admin/Dashboard");

            // Optional: for now, send Manager/Staff to Admin dashboard too
            // Change these later when you create a dedicated Manager queue page
            if (User.IsInRole("Manager") || User.IsInRole("Staff"))
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

            var myRequestsQuery = _context.request
                .AsNoTracking()
                .Include(r => r.status)
                .Where(r => r.created_by == appUser.userID);

            // total count (needed for "View All" logic)
            TotalMyRequests = await myRequestsQuery.CountAsync(cancellationToken);

            // Load all requests once
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