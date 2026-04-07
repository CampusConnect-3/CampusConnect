using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class ManageRequestModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ManageRequestModel> _logger;
        private readonly IActivityLoggerService _activityLogger;

        public ManageRequestModel(
            TablesDbContext context, 
            UserManager<IdentityUser> userManager, 
            ILogger<ManageRequestModel> logger,
            IActivityLoggerService activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _activityLogger = activityLogger;
        }

        public request RequestItem { get; set; } = default!;

        public SelectList StaffOptions { get; set; } = default!;
        public SelectList StatusOptions { get; set; } = default!;
        public SelectList CategoryOptions { get; set; } = default!;

        [BindProperty]
        public int CategoryID { get; set; }

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

        // AJAX handler for dynamic staff filtering
        public async Task<JsonResult> OnGetFilteredStaffAsync(int categoryId)
        {
            var category = await _context.category
                .FirstOrDefaultAsync(c => c.categoryID == categoryId);

            if (category == null)
            {
                return new JsonResult(new List<object>());
            }

            var categoryName = category.categoryName?.Trim();

            var usersInDepartment = await _context.users
                .Where(u => u.status == "Active" 
                    && !string.IsNullOrEmpty(u.identityUserId)
                    && u.department != null
                    && u.department.Trim().ToLower() == categoryName.ToLower())
                .ToListAsync();

            var filteredStaff = new List<object>();

            foreach (var u in usersInDepartment)
            {
                var identityUser = await _userManager.FindByIdAsync(u.identityUserId!);
                if (identityUser != null && await _userManager.IsInRoleAsync(identityUser, "Staff"))
                {
                    filteredStaff.Add(new
                    {
                        u.userID,
                        u.fName,
                        u.lName,
                        u.email,
                        u.department
                    });
                }
            }

            _logger.LogInformation("Filtered {Count} staff for category '{CategoryName}'", filteredStaff.Count, categoryName);

            return new JsonResult(filteredStaff);
        }

        private async Task LoadDropdowns(int categoryID)
        {
            var selectedCategory = await _context.category
                .FirstOrDefaultAsync(c => c.categoryID == categoryID);

            var activeUsers = await _context.users
                .Where(u => u.status == "Active" && !string.IsNullOrEmpty(u.identityUserId))
                .ToListAsync();

            if (selectedCategory != null && !string.IsNullOrEmpty(selectedCategory.categoryName))
            {
                var categoryName = selectedCategory.categoryName.Trim();
                activeUsers = activeUsers
                    .Where(u => u.department != null && u.department.Trim().Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var staffUsers = new List<user>();
            foreach (var u in activeUsers)
            {
                var identityUser = await _userManager.FindByIdAsync(u.identityUserId!);
                if (identityUser != null && await _userManager.IsInRoleAsync(identityUser, "Staff"))
                {
                    staffUsers.Add(u);
                }
            }

            _logger.LogInformation("Loaded {Count} staff members for category {CategoryID}", staffUsers.Count, categoryID);

            var staffDisplay = staffUsers.Select(u => new
            {
                u.userID,
                FullName = $"{u.fName} {u.lName} ({u.department ?? "N/A"})"
            });

            StaffOptions = new SelectList(staffDisplay, "userID", "FullName");

            var categories = await _context.category.OrderBy(c => c.categoryName).ToListAsync();
            CategoryOptions = new SelectList(categories, "categoryID", "categoryName");

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

            CategoryID = req.categoryID;
            AssignedTo = req.assigned_to;
            Priority = req.priority;
            StatusId = req.statusID;
            AssignedDate = req.createdAt;

            await LoadDropdowns(req.categoryID);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var request = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.assignedTo)
                .FirstOrDefaultAsync(r => r.requestID == id);

            if (request == null)
            {
                return NotFound();
            }

            // Track changes for logging
            var oldCategoryId = request.categoryID;
            var oldAssignedTo = request.assigned_to;
            var oldPriority = request.priority;
            var oldStatusId = request.statusID;
            var oldCategory = request.category?.categoryName;
            var oldStatus = request.status?.statusName;
            var oldAssignee = request.assignedTo != null 
                ? $"{request.assignedTo.fName} {request.assignedTo.lName}" 
                : "Unassigned";

            // Update request
            request.categoryID = CategoryID;
            request.assigned_to = AssignedTo;
            request.priority = Priority;
            request.statusID = StatusId;

            await _context.SaveChangesAsync();

            // Reload with new navigation properties
            await _context.Entry(request).Reference(r => r.category).LoadAsync();
            await _context.Entry(request).Reference(r => r.status).LoadAsync();
            await _context.Entry(request).Reference(r => r.assignedTo).LoadAsync();

            // Log category change
            if (oldCategoryId != CategoryID)
            {
                await _activityLogger.LogActivityAsync(
                    action: "changed_category",
                    requestId: request.requestID,
                    requestTitle: request.title,
                    details: new Dictionary<string, object>
                    {
                        { "oldCategory", oldCategory ?? "Unknown" },
                        { "newCategory", request.category?.categoryName ?? "Unknown" }
                    }
                );
            }

            // Log assignment change
            if (oldAssignedTo != AssignedTo)
            {
                var newAssignee = request.assignedTo != null
                    ? $"{request.assignedTo.fName} {request.assignedTo.lName}"
                    : "Unassigned";

                await _activityLogger.LogActivityAsync(
                    action: "reassigned_request",
                    requestId: request.requestID,
                    requestTitle: request.title,
                    details: new Dictionary<string, object>
                    {
                        { "oldAssignee", oldAssignee },
                        { "newAssignee", newAssignee },
                        { "department", request.category?.categoryName ?? "Unknown" }
                    }
                );
            }

            // Log priority change
            if (oldPriority != Priority)
            {
                await _activityLogger.LogActivityAsync(
                    action: "changed_priority",
                    requestId: request.requestID,
                    requestTitle: request.title,
                    details: new Dictionary<string, object>
                    {
                        { "oldPriority", oldPriority ?? "Unknown" },
                        { "newPriority", Priority ?? "Unknown" }
                    }
                );
            }

            // Log status change
            if (oldStatusId != StatusId)
            {
                var newStatus = request.status?.statusName ?? "Unknown";

                await _activityLogger.LogActivityAsync(
                    action: "status_changed",
                    requestId: request.requestID,
                    requestTitle: request.title,
                    details: new Dictionary<string, object>
                    {
                        { "oldStatus", oldStatus ?? "Unknown" },
                        { "newStatus", newStatus }
                    }
                );
            }

            _logger.LogInformation("Manager updated request. RequestId={RequestId}, CategoryId={CategoryId}, AssignedTo={AssignedTo}",
                id, CategoryID, AssignedTo);

            return RedirectToPage("/Manager/Queue");
        }
    }
}