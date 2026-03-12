using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.StaffPages
{
    [Authorize(Roles = nameof(Roles.Staff))]
    public class DashboardModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public user? CurrentUser { get; set; }
        public List<request> ToDoRequests { get; set; } = new();
        public List<request> InProgressRequests { get; set; } = new();
        public List<request> CompleteRequests { get; set; } = new();

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
                TempData["Error"] = "Your user profile does not have a department assigned. Please contact an administrator.";
                return RedirectToPage("/Index");
            }

            // Get only requests assigned to THIS staff member
            var myAssignedRequests = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Where(r => r.assigned_to == CurrentUser.userID) // Only assigned to me
                .OrderByDescending(r => r.createdAt)
                .ToListAsync();

            // Filter by status using constants
            ToDoRequests = myAssignedRequests
                .Where(r => r.status != null && r.status.statusName == RequestStatuses.ToDo)
                .ToList();

            InProgressRequests = myAssignedRequests
                .Where(r => r.status != null && r.status.statusName == RequestStatuses.InProgress)
                .ToList();

            CompleteRequests = myAssignedRequests
                .Where(r => r.status != null && r.status.statusName == RequestStatuses.Closed)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync([FromBody] UpdateStatusRequest updateRequest)
        {
            if (updateRequest == null)
            {
                return BadRequest(new { success = false, message = "Invalid request" });
            }

            var request = await _context.request
                .Include(r => r.status)
                .FirstOrDefaultAsync(r => r.requestID == updateRequest.RequestId);

            if (request == null)
            {
                return NotFound(new { success = false, message = "Request not found" });
            }

            // Find or create the status
            var status = await _context.requestStatus
                .FirstOrDefaultAsync(s => s.statusName == updateRequest.NewStatus);

            if (status == null)
            {
                status = new requestStatus { statusName = updateRequest.NewStatus };
                _context.requestStatus.Add(status);
                await _context.SaveChangesAsync();
            }

            request.statusID = status.statusID;

            // If status is Closed, set closedAt
            if (updateRequest.NewStatus == RequestStatuses.Closed)
            {
                request.closedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // TODO: Trigger notification to user here
            // This is where you'd integrate a notification system

            return new JsonResult(new { success = true });
        }

        public class UpdateStatusRequest
        {
            public int RequestId { get; set; }
            public string NewStatus { get; set; } = string.Empty;
        }
    }
}