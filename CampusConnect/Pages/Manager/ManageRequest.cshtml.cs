using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class ManageRequestModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ManageRequestModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public request RequestItem { get; set; } = default!;

        public SelectList StaffOptions { get; set; } = default!;
        public SelectList StatusOptions { get; set; } = default!;

        [BindProperty]
        public int? AssignedTo { get; set; }

        [BindProperty]
        public string? Priority { get; set; }

        [BindProperty]
        public int? StatusId { get; set; }

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime? AssignedDate { get; set; }

        public List<string> PriorityOptions { get; set; } = new()
        {
            "Low", "Medium", "High", "Critical"
        };

        private async Task LoadDropdowns()
        {
            var activeUsers = await _context.users
                .Where(u => u.status == "Active" && !string.IsNullOrEmpty(u.identityUserId))
                .ToListAsync();

            var staffUsers = new List<user>();

            foreach (var u in activeUsers)
            {
                var identityUser = await _userManager.FindByIdAsync(u.identityUserId!);
                if (identityUser != null && await _userManager.IsInRoleAsync(identityUser, "Staff"))
                {
                    staffUsers.Add(u);
                }
            }

            var staffDisplay = staffUsers.Select(u => new
            {
                u.userID,
                FullName = $"{u.fName} {u.lName}"
            });

            StaffOptions = new SelectList(staffDisplay, "userID", "FullName");

            var statuses = await _context.requestStatus.ToListAsync();
            StatusOptions = new SelectList(statuses, "statusID", "statusName");
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var req = await _context.request
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .FirstOrDefaultAsync(r => r.requestID == id);

            if (req == null)
                return NotFound();

            RequestItem = req;

            AssignedTo = req.assigned_to;
            Priority = req.priority;
            StatusId = req.statusID;
            AssignedDate = req.createdAt; // temporary

            await LoadDropdowns();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var req = await _context.request.FirstOrDefaultAsync(r => r.requestID == id);

            if (req == null)
                return NotFound();

            req.assigned_to = AssignedTo;
            req.priority = Priority ?? req.priority;

            // If a technician is assigned and no status was selected, force In Progress
            if (AssignedTo.HasValue && !StatusId.HasValue)
            {
                var inProgressStatus = await _context.requestStatus
                    .FirstOrDefaultAsync(s => s.statusName == "In Progress");

                if (inProgressStatus != null)
                {
                    req.statusID = inProgressStatus.statusID;
                }
            }
            else
            {
                req.statusID = StatusId;
            }

            // Prevent closing a request if no technician is assigned
            if (!AssignedTo.HasValue)
            {
                var closedStatus = await _context.requestStatus
                    .FirstOrDefaultAsync(s => s.statusName == "Closed");

                if (closedStatus != null && req.statusID == closedStatus.statusID)
                {
                    ModelState.AddModelError(string.Empty, "You cannot close a request that has not been assigned to a technician.");

                    var reloaded = await _context.request
                        .Include(r => r.createdBy)
                        .Include(r => r.assignedTo)
                        .Include(r => r.status)
                        .Include(r => r.category)
                        .FirstOrDefaultAsync(r => r.requestID == id);

                    if (reloaded == null)
                        return NotFound();

                    RequestItem = reloaded;
                    await LoadDropdowns();
                    return Page();
                }
            }

            if (AssignedDate.HasValue)
                req.createdAt = AssignedDate.Value;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Manager/Queue");
        }
    }
}