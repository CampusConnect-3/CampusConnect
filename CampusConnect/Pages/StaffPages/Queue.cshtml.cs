using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.StaffPages
{
    [Authorize(Roles = nameof(Roles.Staff))]
    public class QueueModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IActivityLoggerService _activityLogger;

        public QueueModel(
            TablesDbContext context, 
            UserManager<IdentityUser> userManager,
            IActivityLoggerService activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        public user? CurrentUser { get; set; }
        public List<request> AssignedRequests { get; set; } = new();
        public List<request> UnassignedRequests { get; set; } = new();
        public List<request> AllDepartmentRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            CurrentUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUser.Id);

            if (CurrentUser?.department == null)
            {
                return RedirectToPage("/Error");
            }

            // Get all requests for this department
            var departmentRequests = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Where(r => r.category!.categoryName == CurrentUser.department)
                .ToListAsync();

            // Define priority order for sorting
            var priorityOrder = new Dictionary<string, int>
            {
                { "Critical", 1 },
                { "High", 2 },
                { "Medium", 3 },
                { "Low", 4 }
            };

            // Sort all lists by priority first, then by date
            AllDepartmentRequests = departmentRequests
                .OrderBy(r => priorityOrder.GetValueOrDefault(r.priority, 999))
                .ThenByDescending(r => r.createdAt)
                .ToList();

            // Filter assigned to current user (still priority sorted)
            AssignedRequests = departmentRequests
                .Where(r => r.assigned_to == CurrentUser.userID)
                .OrderBy(r => priorityOrder.GetValueOrDefault(r.priority, 999))
                .ThenByDescending(r => r.createdAt)
                .ToList();

            // Filter unassigned (still priority sorted)
            UnassignedRequests = departmentRequests
                .Where(r => r.assigned_to == null)
                .OrderBy(r => priorityOrder.GetValueOrDefault(r.priority, 999))
                .ThenByDescending(r => r.createdAt)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAssignToMeAsync(int requestId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            var currentUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUser.Id);

            var request = await _context.request
                .Include(r => r.category)
                .FirstOrDefaultAsync(r => r.requestID == requestId);
                
            if (request == null)
            {
                return NotFound();
            }

            request.assigned_to = currentUser!.userID;
            await _context.SaveChangesAsync();

            // LOG THE ACTIVITY
            await _activityLogger.LogActivityAsync(
                action: "assigned_request",
                requestId: request.requestID,
                requestTitle: request.title,
                details: new Dictionary<string, object>
                {
                    { "assignedTo", $"{currentUser.fName} {currentUser.lName}" },
                    { "department", request.category?.categoryName ?? "Unknown" }
                }
            );

            return RedirectToPage();
        }
    }
}