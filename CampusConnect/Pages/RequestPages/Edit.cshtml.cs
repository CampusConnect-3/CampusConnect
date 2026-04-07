using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "Admin,Manager")]
    public class EditModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<EditModel> _logger;
        private readonly INotificationService _notifications;
        private readonly IActivityLoggerService _activityLogger;

        public EditModel(
            TablesDbContext context, 
            ILogger<EditModel> logger, 
            INotificationService notifications,
            IActivityLoggerService activityLogger)
        {
            _context = context;
            _logger = logger;
            _notifications = notifications;
            _activityLogger = activityLogger;
        }

        [BindProperty]
        public request request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }

            var req = await _context.request
                .Include(r => r.category)
                .FirstOrDefaultAsync(m => m.requestID == id, cancellationToken);
            
            if (req == null) return NotFound();

            this.request = req;

            await PopulateDropdownsAsync(req.categoryID, cancellationToken);
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(request.categoryID, cancellationToken);
                return Page();
            }

            var existing = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.requestID == request.requestID, cancellationToken);

            if (existing == null)
                return NotFound();

            // Track changes
            var titleChanged = existing.title != request.title;
            var descriptionChanged = existing.description != request.description;
            var priorityChanged = existing.priority != request.priority;
            var statusChanged = existing.statusID != request.statusID;
            var assigneeChanged = existing.assigned_to != request.assigned_to;
            var categoryChanged = existing.categoryID != request.categoryID;

            request.created_by = existing.created_by;
            request.createdAt = existing.createdAt;

            var recipientIdentityUserId = await _context.users
                .AsNoTracking()
                .Where(u => u.userID == request.created_by)
                .Select(u => u.identityUserId)
                .FirstOrDefaultAsync(cancellationToken);

            var assigneeIdentityUserId = request.assigned_to.HasValue
                ? await _context.users
                    .AsNoTracking()
                    .Where(u => u.userID == request.assigned_to.Value)
                    .Select(u => u.identityUserId)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            var requesterName = await _context.users
                .AsNoTracking()
                .Where(u => u.userID == request.created_by)
                .Select(u => ((u.fName + " " + u.lName).Trim()) == "" ? u.email : (u.fName + " " + u.lName).Trim())
                .FirstOrDefaultAsync(cancellationToken) ?? "A student";

            _context.Entry(request).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);

                // Reload navigation properties
                await _context.Entry(request).Reference(r => r.category).LoadAsync(cancellationToken);
                await _context.Entry(request).Reference(r => r.status).LoadAsync(cancellationToken);

                // Log title change
                if (titleChanged)
                {
                    await _activityLogger.LogActivityAsync(
                        action: "edited_request_title",
                        requestId: request.requestID,
                        requestTitle: request.title,
                        details: new Dictionary<string, object>
                        {
                            { "oldTitle", existing.title },
                            { "newTitle", request.title }
                        }
                    );
                }

                // Log description change
                if (descriptionChanged)
                {
                    await _activityLogger.LogActivityAsync(
                        action: "edited_request_description",
                        requestId: request.requestID,
                        requestTitle: request.title,
                        details: new Dictionary<string, object>
                        {
                            { "changed", "Description updated" }
                        }
                    );
                }

                // Log priority change
                if (priorityChanged)
                {
                    await _activityLogger.LogActivityAsync(
                        action: "changed_priority",
                        requestId: request.requestID,
                        requestTitle: request.title,
                        details: new Dictionary<string, object>
                        {
                            { "oldPriority", existing.priority },
                            { "newPriority", request.priority }
                        }
                    );
                }

                // Log category change
                if (categoryChanged)
                {
                    await _activityLogger.LogActivityAsync(
                        action: "changed_category",
                        requestId: request.requestID,
                        requestTitle: request.title,
                        details: new Dictionary<string, object>
                        {
                            { "oldCategory", existing.category?.categoryName ?? "Unknown" },
                            { "newCategory", request.category?.categoryName ?? "Unknown" }
                        }
                    );
                }

                // Log status change
                if (statusChanged)
                {
                    await _activityLogger.LogActivityAsync(
                        action: "status_changed",
                        requestId: request.requestID,
                        requestTitle: request.title,
                        details: new Dictionary<string, object>
                        {
                            { "oldStatus", existing.status?.statusName ?? "Unknown" },
                            { "newStatus", request.status?.statusName ?? "Unknown" }
                        }
                    );

                    if (!string.IsNullOrEmpty(recipientIdentityUserId))
                    {
                        await _notifications.CreateStatusChangedNotificationAsync(
                            request.requestID,
                            recipientIdentityUserId,
                            request.statusID,
                            cancellationToken);
                    }
                }

                // Log assignment change
                if (assigneeChanged)
                {
                    var oldAssignee = existing.assigned_to.HasValue 
                        ? await _context.users
                            .Where(u => u.userID == existing.assigned_to.Value)
                            .Select(u => $"{u.fName} {u.lName}")
                            .FirstOrDefaultAsync(cancellationToken) ?? "Unassigned"
                        : "Unassigned";

                    var newAssignee = request.assigned_to.HasValue
                        ? await _context.users
                            .Where(u => u.userID == request.assigned_to.Value)
                            .Select(u => $"{u.fName} {u.lName}")
                            .FirstOrDefaultAsync(cancellationToken) ?? "Unassigned"
                        : "Unassigned";

                    await _activityLogger.LogActivityAsync(
                        action: "reassigned_request",
                        requestId: request.requestID,
                        requestTitle: request.title,
                        details: new Dictionary<string, object>
                        {
                            { "oldAssignee", oldAssignee },
                            { "newAssignee", newAssignee }
                        }
                    );

                    if (request.assigned_to.HasValue && !string.IsNullOrEmpty(recipientIdentityUserId))
                    {
                        await _notifications.CreateStudentAssignmentNotificationAsync(
                            request.requestID,
                            recipientIdentityUserId,
                            request.assigned_to.Value,
                            cancellationToken);
                    }

                    if (request.assigned_to.HasValue && !string.IsNullOrEmpty(assigneeIdentityUserId))
                    {
                        await _notifications.CreateStaffAssignmentNotificationAsync(
                            request.requestID,
                            assigneeIdentityUserId,
                            requesterName,
                            cancellationToken);
                    }
                }

                _logger.LogInformation("Request edited. RequestId={RequestId} UserId={UserId}",
                    request.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict editing request. RequestId={RequestId}",
                    request.requestID
                );

                if (!requestExists(request.requestID))
                    return NotFound();

                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool requestExists(int id)
        {
            return _context.request.Any(e => e.requestID == id);
        }

        private async Task PopulateDropdownsAsync(int categoryID, CancellationToken cancellationToken = default)
        {
            var categories = await _context.category.ToListAsync(cancellationToken);
            ViewData["categoryID"] = new SelectList(categories, "categoryID", "categoryName");

            var statuses = await _context.requestStatus.ToListAsync(cancellationToken);
            ViewData["statusID"] = new SelectList(statuses, "statusID", "statusName");

            var staff = await _context.users
                .Where(u => u.status == "Active")
                .ToListAsync(cancellationToken);
            ViewData["assigned_to"] = new SelectList(staff, "userID", "fName");
        }
    }
}
