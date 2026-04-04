using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CampusConnect.Pages.StaffPages
{
    [Authorize(Roles = "Staff")]
    public class RequestDetailModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RequestDetailModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public request RequestItem { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int requestId)
        {
            RequestItem = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.comments)
                    .ThenInclude(c => c.creator)
                .Include(r => r.attachments)
                    .ThenInclude(a => a.creator)
                .FirstOrDefaultAsync(r => r.requestID == requestId);

            if (RequestItem == null)
            {
                return NotFound();
            }

            return Partial("_RequestDetail", RequestItem);
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int requestId, string commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                return BadRequest("Comment text is required.");
            }

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            var currentUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUser.Id);

            var comment = new requestComments
            {
                requestID = requestId,
                commentText = commentText.Trim(),
                createdAt = DateTime.Now,
                creatorID = currentUser!.userID
            };

            await EnsureCommentIdAsync(comment);

            _context.requestComments.Add(comment);
            await _context.SaveChangesAsync();

            RequestItem = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.comments)
                    .ThenInclude(c => c.creator)
                .Include(r => r.attachments)
                    .ThenInclude(a => a.creator)
                .FirstOrDefaultAsync(r => r.requestID == requestId);

            if (RequestItem == null)
            {
                return NotFound();
            }

            return Partial("_RequestDetail", RequestItem);
        }

        public async Task<IActionResult> OnPostAddAttachmentAsync(int requestId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            var currentUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUser.Id);

            // Save file to wwwroot/uploads
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var attachment = new attachments
            {
                requestID = requestId,
                fileName = file.FileName,
                contentType = file.ContentType,
                fileUrl = $"/uploads/{uniqueFileName}",
                uploadedAt = DateTime.Now,
                creatorID = currentUser!.userID
            };

            _context.attachments.Add(attachment);
            await _context.SaveChangesAsync();

            // TODO: Trigger notification to request creator

            // Reload the request with updated data and return partial view
            RequestItem = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.comments)
                    .ThenInclude(c => c.creator)
                .Include(r => r.attachments)
                    .ThenInclude(a => a.creator)
                .FirstOrDefaultAsync(r => r.requestID == requestId);

            return Partial("_RequestDetail", RequestItem);
        }

        private async Task EnsureCommentIdAsync(requestComments comment)
        {
            const string isIdentitySql = """
                SELECT CAST(COLUMNPROPERTY(OBJECT_ID('dbo.requestComments'), 'commentID', 'IsIdentity') AS int)
                """;

            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = isIdentitySql;

                if (command is SqlCommand sqlCommand)
                    sqlCommand.CommandTimeout = 30;

                var result = await command.ExecuteScalarAsync();
                var isIdentity = result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;

                if (!isIdentity)
                {
                    comment.commentID = await _context.requestComments
                        .AsNoTracking()
                        .Select(c => (int?)c.commentID)
                        .MaxAsync() is int maxCommentId
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
    }
}
