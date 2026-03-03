using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    [Authorize(Roles = "Admin,Manager,Staff,User")]
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

        public IActionResult OnGet()
        {
            PopulateDropdowns();
            request = new request
            {
                email = User.Identity?.Name ?? ""
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return Page();
            }

            // FORCE EMAIL SERVER-SIDE (never trust UI)
            request.email = User.Identity?.Name ?? request.email;

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

        private void PopulateDropdowns()
        {
            ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");

            ViewData["priority"] = new SelectList(new[]
            {
                new { Value = "Low", Text = "Low" },
                new { Value = "Medium", Text = "Medium" },
                new { Value = "High", Text = "High" },
                new { Value = "Critical", Text = "Critical" }
            }, "Value", "Text");
        }
    }
}