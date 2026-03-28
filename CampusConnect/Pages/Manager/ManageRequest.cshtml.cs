using CampusConnect.Data;
using CampusConnect.Models;
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

        public ManageRequestModel(TablesDbContext context, UserManager<IdentityUser> userManager, ILogger<ManageRequestModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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

            // Get all active users with matching department
            var usersInDepartment = await _context.users
                .Where(u => u.status == "Active" 
                    && !string.IsNullOrEmpty(u.identityUserId)
                    && u.department != null
                    && u.department.Trim().ToLower() == categoryName.ToLower())
                .ToListAsync();

            var filteredStaff = new List<object>();

            // Filter by Staff role
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
            // Get the category to filter staff
            var selectedCategory = await _context.category
                .FirstOrDefaultAsync(c => c.categoryID == categoryID);

            // Get all active users
            var activeUsers = await _context.users
                .Where(u => u.status == "Active" && !string.IsNullOrEmpty(u.identityUserId))
                .ToListAsync();

            // Filter by department if category has one
            if (selectedCategory != null && !string.IsNullOrEmpty(selectedCategory.categoryName))
            {
                var categoryName = selectedCategory.categoryName.Trim();
                activeUsers = activeUsers
                    .Where(u => u.department != null && u.department.Trim().Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filter by Staff role
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

            // Load categories and statuses
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
            AssignedDate = req.createdAt; // temporary

            await LoadDropdowns(req.categoryID);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                var reloadedForValidation = await _context.request
                    .Include(r => r.createdBy)
                    .Include(r => r.assignedTo)
                    .Include(r => r.status)
                    .Include(r => r.category)
                    .FirstOrDefaultAsync(r => r.requestID == id);

                if (reloadedForValidation == null)
                    return NotFound();

                RequestItem = reloadedForValidation;
                await LoadDropdowns(CategoryID);
                return Page();
            }

            var req = await _context.request.FirstOrDefaultAsync(r => r.requestID == id);

            if (req == null)
                return NotFound();

            // Update category
            req.categoryID = CategoryID;
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
                    await LoadDropdowns(CategoryID);
                    return Page();
                }
            }

            if (AssignedDate.HasValue)
                req.createdAt = AssignedDate.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Manager updated request. RequestId={RequestId}, CategoryId={CategoryId}, AssignedTo={AssignedTo}",
                id, CategoryID, AssignedTo);

            return RedirectToPage("/Manager/Queue");
        }
    }
}