using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "Admin,Manager,Staff,User")]
    public class DetailsModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private readonly INotificationService _notifications;

        public DetailsModel(TablesDbContext context, ILogger<DetailsModel> logger, INotificationService notifications)
        {
            _context = context;
            _logger = logger;
            _notifications = notifications;
        }

        public request request { get; private set; } = default!;
        public List<attachments> Attachments { get; private set; } = new();
        public List<requestComments> Comments { get; private set; } = new();

        [BindProperty]
        public NewCommentInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            try
            {
                var req = await _context.request
                    .AsNoTracking()
                    .Include(r => r.createdBy)
                    .Include(r => r.assignedTo)
                    .Include(r => r.status)
                    .Include(r => r.category)
                    .FirstOrDefaultAsync(m => m.requestID == id, cancellationToken);

                if (req == null)
                {
                    _logger.LogWarning("DETAILS NOT FOUND. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                        id,
                        User.FindFirstValue(ClaimTypes.NameIdentifier),
                        HttpContext.TraceIdentifier);
                    return NotFound();
                }

                if (!await CanAccessRequestAsync(req, cancellationToken))
                    return Forbid();

                request = req;
                await LoadRelatedDataAsync(req.requestID, cancellationToken);

                // Log as Information (view access)
                _logger.LogInformation("DETAILS VIEWED. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    HttpContext.TraceIdentifier);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DETAILS GET FAILED. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    HttpContext.TraceIdentifier);
                throw; // global exception handler => /Error
            }
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            var req = await _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .FirstOrDefaultAsync(r => r.requestID == id, cancellationToken);

            if (req == null)
                return NotFound();

            if (!await CanAccessRequestAsync(req, cancellationToken))
                return Forbid();

            request = req;
            await LoadRelatedDataAsync(req.requestID, cancellationToken);

            if (string.IsNullOrWhiteSpace(Input.CommentText))
            {
                ModelState.AddModelError("Input.CommentText", "Comment text is required.");
            }

            if (!ModelState.IsValid)
                return Page();

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
                return Forbid();

            var appUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId, cancellationToken);

            if (appUser == null)
                return Forbid();

            var comment = new requestComments
            {
                requestID = req.requestID,
                creatorID = appUser.userID,
                commentText = Input.CommentText.Trim(),
                createdAt = DateTime.UtcNow
            };

            _context.requestComments.Add(comment);
            await _context.SaveChangesAsync(cancellationToken);

            if ((User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff"))
                && req.created_by != appUser.userID)
            {
                var recipientIdentityUserId = await _context.users
                    .AsNoTracking()
                    .Where(u => u.userID == req.created_by)
                    .Select(u => u.identityUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrEmpty(recipientIdentityUserId))
                {
                    await _notifications.CreateStaffCommentNotificationAsync(
                        req.requestID,
                        recipientIdentityUserId,
                        appUser.userID,
                        comment.commentText ?? string.Empty,
                        cancellationToken);
                }
            }

            if (User.IsInRole("User") && req.assigned_to.HasValue && req.assigned_to.Value != appUser.userID)
            {
                var staffRecipientIdentityUserId = await _context.users
                    .AsNoTracking()
                    .Where(u => u.userID == req.assigned_to.Value)
                    .Select(u => u.identityUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                var studentName = ((appUser.fName + " " + appUser.lName).Trim()) == ""
                    ? appUser.email
                    : (appUser.fName + " " + appUser.lName).Trim();

                if (!string.IsNullOrEmpty(staffRecipientIdentityUserId))
                {
                    await _notifications.CreateStudentCommentNotificationAsync(
                        req.requestID,
                        staffRecipientIdentityUserId,
                        studentName,
                        comment.commentText ?? string.Empty,
                        cancellationToken);
                }
            }

            _logger.LogInformation("COMMENT ADDED. RequestId={RequestId} CommentId={CommentId} UserId={UserId} TraceId={TraceId}",
                req.requestID,
                comment.commentID,
                identityUserId,
                HttpContext.TraceIdentifier);
            return RedirectToPage(new { id = req.requestID });
        }

        private async Task LoadRelatedDataAsync(int requestId, CancellationToken cancellationToken)
        {
            Attachments = await _context.attachments
                .AsNoTracking()
                .Where(a => a.requestID == requestId)
                .Include(a => a.creator)
                .OrderByDescending(a => a.uploadedAt)
                .ToListAsync(cancellationToken);

            Comments = await _context.requestComments
                .AsNoTracking()
                .Where(c => c.requestID == requestId)
                .Include(c => c.creator)
                .OrderBy(c => c.createdAt)
                .ToListAsync(cancellationToken);
        }

        private async Task<bool> CanAccessRequestAsync(request req, CancellationToken cancellationToken)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff"))
                return true;

            if (!User.IsInRole("User"))
                return false;

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
                return false;

            var appUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId, cancellationToken);

            return appUser != null && req.created_by == appUser.userID;
        }

        public class NewCommentInput
        {
            [Required]
            [StringLength(1000)]
            public string CommentText { get; set; } = string.Empty;
        }
    }
}
