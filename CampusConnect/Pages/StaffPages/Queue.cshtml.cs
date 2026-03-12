using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Pages.StaffPages
{
    [Authorize(Roles = nameof(Roles.Staff))]
    public class QueueModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public QueueModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            AllDepartmentRequests = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Where(r => r.category!.categoryName == CurrentUser.department)
                .OrderByDescending(r => r.createdAt)
                .ToListAsync();

            // Filter assigned to current user
            AssignedRequests = AllDepartmentRequests
                .Where(r => r.assigned_to == CurrentUser.userID)
                .ToList();

            // Filter unassigned
            UnassignedRequests = AllDepartmentRequests
                .Where(r => r.assigned_to == null)
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

            var request = await _context.request.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            request.assigned_to = currentUser!.userID;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}