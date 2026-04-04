using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "User")]
    public class CreateModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private readonly IWebHostEnvironment _env;

        public CreateModel(TablesDbContext context, ILogger<CreateModel> logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        [BindProperty]
        public request request { get; set; } = default!;

        [BindProperty]
        public List<IFormFile>? Attachments { get; set; }

        public string DisplayName { get; private set; } = "User";
        public string StudentId { get; private set; } = "N/A";

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            await LoadHeaderAsync(cancellationToken);

            request = new request
            {
                // prefill (still enforced server-side on post)
                email = User.Identity?.Name ?? ""
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            await LoadHeaderAsync(cancellationToken);

            // FORCE EMAIL SERVER-SIDE (never trust UI)
            request.email = User.Identity?.Name ?? request.email;

            if (!await ApplySystemManagedDefaultsAsync(cancellationToken))
                return Page();

            ModelState.Remove("request.priority");
            ModelState.Remove("request.categoryID");
            ModelState.Remove("request.statusID");

            if (!TryValidateModel(request, nameof(request)))
                return Page();

            // get Identity ID
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
                return Forbid();

            var appUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId, cancellationToken);

            if (appUser == null)
                return Forbid();

            request.created_by = appUser.userID;
            request.createdAt = DateTime.UtcNow;
            request.closedAt = null;

            // Save request first to get the requestID
            _context.request.Add(request);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Request created. RequestId={RequestId} UserId={UserId}",
                request.requestID,
                identityUserId
            );

            // NOW handle attachments (after we have a requestID)
            if (Attachments != null && Attachments.Any())
            {
                var uploadedCount = await ProcessAttachmentsAsync(request.requestID, appUser.userID, cancellationToken);
                _logger.LogInformation(
                    "Uploaded {Count} attachments for RequestId={RequestId}",
                    uploadedCount,
                    request.requestID
                );
            }

            return RedirectToPage("/Index");
        }

        private async Task<int> ProcessAttachmentsAsync(int requestId, int creatorUserId, CancellationToken cancellationToken)
        {
            if (Attachments == null || !Attachments.Any())
                return 0;

            var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".pdf", ".doc", ".docx" };
            const long maxBytes = 10 * 1024 * 1024; // 10MB per file

            var uploadedCount = 0;
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "requests", requestId.ToString());
            Directory.CreateDirectory(uploadFolder);

            foreach (var file in Attachments)
            {
                if (file == null || file.Length == 0)
                    continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExts.Contains(ext))
                {
                    _logger.LogWarning("Skipping file {FileName} - invalid extension", file.FileName);
                    continue;
                }

                if (file.Length > maxBytes)
                {
                    _logger.LogWarning("Skipping file {FileName} - too large ({Size} bytes)", file.FileName, file.Length);
                    continue;
                }

                try
                {
                    var safeFileName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadFolder, safeFileName);

                    await using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }

                    var attachment = new attachments
                    {
                        requestID = requestId,
                        creatorID = creatorUserId,
                        fileName = file.FileName,
                        contentType = file.ContentType,
                        fileUrl = $"/uploads/requests/{requestId}/{safeFileName}",
                        uploadedAt = DateTime.UtcNow
                    };

                    _context.attachments.Add(attachment);
                    uploadedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload attachment {FileName} for RequestId={RequestId}",
                        file.FileName, requestId);
                }
            }

            if (uploadedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return uploadedCount;
        }

        private async Task LoadHeaderAsync(CancellationToken cancellationToken)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            DisplayName = User.Identity?.Name ?? "User";
            StudentId = User.FindFirst("student_id")?.Value ?? "N/A";

            if (string.IsNullOrWhiteSpace(identityUserId))
                return;

            var appUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId, cancellationToken);

            if (appUser == null)
                return;

            var fullName = $"{appUser.fName} {appUser.lName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                DisplayName = fullName;
            else if (!string.IsNullOrWhiteSpace(appUser.email))
                DisplayName = appUser.email;

            if (!string.IsNullOrWhiteSpace(appUser.username))
                StudentId = appUser.username;
        }

        private async Task<bool> ApplySystemManagedDefaultsAsync(CancellationToken cancellationToken)
        {
            request.priority = string.Empty;

            if (request.categoryID <= 0)
            {
                var preferredCategoryNames = new[] { "General", "General Support", "Triage", "Uncategorized", "Other" };

                var defaultCategoryId = await _context.category
                    .AsNoTracking()
                    .OrderBy(c => preferredCategoryNames.Contains(c.categoryName) ? 0 : 1)
                    .ThenBy(c => c.categoryName)
                    .Select(c => (int?)c.categoryID)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!defaultCategoryId.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "No request categories are configured yet. Please contact support.");
                    return false;
                }

                request.categoryID = defaultCategoryId.Value;
            }

            if (!request.statusID.HasValue)
            {
                request.statusID = await _context.requestStatus
                    .AsNoTracking()
                    .Where(s => s.statusName == Constants.RequestStatuses.ToDo)
                    .Select(s => (int?)s.statusID)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return true;
        }
    }
}
