using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class QueueModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public QueueModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<request> Requests { get; set; } = new List<request>();

        public SelectList StaffOptions { get; set; } = default!;
        public SelectList StatusOptions { get; set; } = default!;

        [BindProperty]
        public int RequestId { get; set; }

        [BindProperty]
        public int? AssignedTo { get; set; }

        [BindProperty]
        public string? Priority { get; set; }

        [BindProperty]
        public int? StatusId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<string> PriorityOptions { get; set; } = new()
        {
            "Low",
            "Medium",
            "High",
            "Critical"
        };

        private async Task LoadPageAsync()
        {
            var query = _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .Where(r => r.status == null || r.status.statusName != "Closed")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();

                query = query.Where(r =>
                    r.title.Contains(term) ||
                    r.requestID.ToString().Contains(term) ||
                    (r.createdBy != null && (
                        r.createdBy.email.Contains(term) ||
                        (r.createdBy.fName + " " + r.createdBy.lName).Contains(term)
                    )));
            }

            Requests = await query
                .OrderByDescending(r => r.createdAt)
                .ToListAsync();

            var activeUsers = await _context.users
                .AsNoTracking()
                .Where(u => u.status == "Active" && !string.IsNullOrEmpty(u.identityUserId))
                .OrderBy(u => u.fName)
                .ThenBy(u => u.lName)
                .ToListAsync();

            var staffUsers = new List<user>();

            foreach (var appUser in activeUsers)
            {
                var identityUser = await _userManager.FindByIdAsync(appUser.identityUserId!);
                if (identityUser != null && await _userManager.IsInRoleAsync(identityUser, "Staff"))
                {
                    staffUsers.Add(appUser);
                }
            }

            var staffDisplay = staffUsers.Select(u => new
            {
                u.userID,
                FullName = $"{u.fName} {u.lName}".Trim()
            }).ToList();

            var statuses = await _context.requestStatus
                .AsNoTracking()
                .OrderBy(s => s.statusName)
                .ToListAsync();

            StaffOptions = new SelectList(staffDisplay, "userID", "FullName");
            StatusOptions = new SelectList(statuses, "statusID", "statusName");
        }

        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }
    }
}