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
        private readonly CampusConnect.Data.TablesDbContext _context;
        private readonly ILogger<EditModel> _logger;
        private readonly INotificationService _notifications;

        public EditModel(TablesDbContext context, ILogger<EditModel> logger, INotificationService notifications)
        {
            _context = context;
            _logger = logger;
            _notifications = notifications;
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

            // SECURITY: Prevent created_by from being tampered with
            var existing = await _context.request.AsNoTracking()
                .FirstOrDefaultAsync(r => r.requestID == request.requestID, cancellationToken);

            if (existing == null)
                return NotFound();

            request.created_by = existing.created_by;
            request.createdAt = existing.createdAt;

            var statusChanged = existing.statusID != request.statusID;
            var assigneeChanged = existing.assigned_to != request.assigned_to;

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

                if (statusChanged && !string.IsNullOrEmpty(recipientIdentityUserId))
                {
                    await _notifications.CreateStatusChangedNotificationAsync(
                        request.requestID,
                        recipientIdentityUserId,
                        request.statusID,
                        cancellationToken);
                }

                if (assigneeChanged && request.assigned_to.HasValue && !string.IsNullOrEmpty(recipientIdentityUserId))
                {
                    await _notifications.CreateStudentAssignmentNotificationAsync(
                        request.requestID,
                        recipientIdentityUserId,
                        request.assigned_to.Value,
                        cancellationToken);
                }

                if (assigneeChanged && request.assigned_to.HasValue && !string.IsNullOrEmpty(assigneeIdentityUserId))
                {
                    await _notifications.CreateStaffAssignmentNotificationAsync(
                        request.requestID,
                        assigneeIdentityUserId,
                        requesterName,
                        cancellationToken);
                }

                _logger.LogInformation("CRITICAL: Request edited. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    request.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict editing request. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    request.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );

                if (!requestExists(request.requestID))
                    return NotFound();

                throw;
            }

            return RedirectToPage("./Index");
        }

        // AJAX handler for dynamic staff filtering when category changes
        public async Task<JsonResult> OnGetFilteredStaffAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            var category = await _context.category
                .FirstOrDefaultAsync(c => c.categoryID == categoryId, cancellationToken);

            if (category == null)
            {
                return new JsonResult(new List<object>());
            }

            var categoryName = category.categoryName?.Trim();

            // Filter users where department matches the category name (case-insensitive)
            var filteredStaff = await _context.users
                .Where(u => u.department != null && u.department.Trim().ToLower() == categoryName.ToLower())
                .Select(u => new
                {
                    u.userID,
                    u.email,
                    u.fName,
                    u.lName,
                    u.department
                })
                .OrderBy(u => u.fName)
                .ThenBy(u => u.lName)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Filtered {Count} staff members for category '{CategoryName}'", 
                filteredStaff.Count, categoryName);

            return new JsonResult(filteredStaff);
        }

        private async Task PopulateDropdownsAsync(int categoryID, CancellationToken cancellationToken = default)
        {
            // Get the category to filter staff by matching department
            var selectedCategory = await _context.category
                .FirstOrDefaultAsync(c => c.categoryID == categoryID, cancellationToken);

            // Filter staff by department matching category name (case-insensitive, trimmed)
            var staffQuery = _context.users.AsQueryable();
            
            if (selectedCategory != null)
            {
                var categoryName = selectedCategory.categoryName?.Trim();
                
                // Match user.department with category.categoryName (case-insensitive)
                staffQuery = staffQuery.Where(u => 
                    u.department != null && 
                    u.department.Trim().ToLower() == categoryName.ToLower()
                );

                // Log for debugging
                _logger.LogInformation("Filtering staff by category: {CategoryName}", categoryName);
            }

            var filteredStaff = await staffQuery
                .OrderBy(u => u.fName)
                .ThenBy(u => u.lName)
                .Select(u => new
                {
                    u.userID,
                    DisplayText = $"{u.fName} {u.lName} ({u.email}) - {u.department}"
                })
                .ToListAsync(cancellationToken);

            // Log the count for debugging
            _logger.LogInformation("Found {Count} staff members for category {CategoryId}", 
                filteredStaff.Count, categoryID);

            ViewData["assigned_to"] = new SelectList(
                filteredStaff,
                "userID",
                "DisplayText",
                request?.assigned_to
            );

            ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
            ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
            ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");
        }

        private bool requestExists(int id)
        {
            return _context.request.Any(e => e.requestID == id);
        }
    }
}
