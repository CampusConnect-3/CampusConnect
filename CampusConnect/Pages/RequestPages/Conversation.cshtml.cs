using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "Admin,Manager,Staff,User")]
    public class ConversationModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<ConversationModel> _logger;
        private readonly INotificationService _notifications;

        public ConversationModel(TablesDbContext context, ILogger<ConversationModel> logger, INotificationService notifications)
        {
            _context = context;
            _logger = logger;
            _notifications = notifications;
        }

        public request RequestItem { get; private set; } = default!;
        public List<requestComments> Comments { get; private set; } = new();

        [BindProperty]
        public NewCommentInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            var req = await LoadRequestAsync(id.Value, cancellationToken);
            if (req == null)
                return NotFound();

            if (!await CanAccessRequestAsync(req, cancellationToken))
                return Forbid();

            RequestItem = req;
            await LoadCommentsAsync(req.requestID, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            var req = await LoadRequestAsync(id.Value, cancellationToken);
            if (req == null)
                return NotFound();

            if (!await CanAccessRequestAsync(req, cancellationToken))
                return Forbid();

            RequestItem = req;
            await LoadCommentsAsync(req.requestID, cancellationToken);

            if (string.IsNullOrWhiteSpace(Input.CommentText))
                ModelState.AddModelError("Input.CommentText", "Comment text is required.");

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

            await EnsureCommentIdAsync(comment, cancellationToken);

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

                var studentName = GetDisplayName(appUser, appUser.email);

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

        private async Task<request?> LoadRequestAsync(int requestId, CancellationToken cancellationToken)
        {
            return await _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .FirstOrDefaultAsync(r => r.requestID == requestId, cancellationToken);
        }

        private async Task LoadCommentsAsync(int requestId, CancellationToken cancellationToken)
        {
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

        private async Task EnsureCommentIdAsync(requestComments comment, CancellationToken cancellationToken)
        {
            const string isIdentitySql = """
                SELECT CAST(COLUMNPROPERTY(OBJECT_ID('dbo.requestComments'), 'commentID', 'IsIdentity') AS int)
                """;

            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = isIdentitySql;

                if (command is SqlCommand sqlCommand)
                    sqlCommand.CommandTimeout = 30;

                var result = await command.ExecuteScalarAsync(cancellationToken);
                var isIdentity = result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;

                if (!isIdentity)
                {
                    comment.commentID = await _context.requestComments
                        .AsNoTracking()
                        .Select(c => (int?)c.commentID)
                        .MaxAsync(cancellationToken) is int maxCommentId
                        ? maxCommentId + 1
                        : 1;
                }
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        public string GetDisplayName(user? person, string fallback = "Unknown user")
        {
            if (person == null)
                return fallback;

            var fullName = $"{person.fName} {person.lName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            if (!string.IsNullOrWhiteSpace(person.email))
                return person.email;

            return fallback;
        }

        public bool IsRequesterComment(requestComments comment)
        {
            return comment.creatorID == RequestItem.created_by;
        }

        public class NewCommentInput
        {
            [Required]
            [StringLength(1000)]
            public string CommentText { get; set; } = string.Empty;
        }
    }
}
