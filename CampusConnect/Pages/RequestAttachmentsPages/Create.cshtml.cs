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
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestAttachmentsPages
{
    [Authorize(Roles = "Manager,Staff,User")]
    public class CreateModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private readonly IWebHostEnvironment _env;

        public CreateModel(
            TablesDbContext context,
            ILogger<CreateModel> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public int? RequestId { get; set; }

        [BindProperty]
        public IFormFile? Upload { get; set; }

        public async Task<IActionResult> OnGetAsync(int? requestId, CancellationToken cancellationToken = default)
        {
            RequestId = requestId;

            if (RequestId == null)
                return NotFound("Missing requestId.");

            if (!await CanAccessRequestAsync(RequestId.Value))
                return Forbid();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (RequestId == null)
            {
                return Page();
            }

            ViewData["creatorID"] = new SelectList(_context.users, "userID", "email");
            ViewData["requestID"] = new SelectList(_context.request, "requestID", "buildingName");
            return Page();
        }

            // Allowed extensions
            var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".pdf" };
            var ext = Path.GetExtension(Upload.FileName).ToLowerInvariant();

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (RequestId == null)
            {
                ModelState.AddModelError("", "Request ID is required.");
                return Page();
            }

            if (Upload == null || Upload.Length == 0)
            {
                ModelState.AddModelError("Upload", "Please select a file to upload.");
                return Page();
            }

            // Validate file size (5MB)
            if (Upload.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("Upload", "File size cannot exceed 5MB.");
                return Page();
            }

            // Validate file type
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".pdf" };
            var extension = Path.GetExtension(Upload.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)  )
            {
                ModelState.AddModelError(string.Empty, "File too large (max 5MB).");
                return Page();
            }

            // Get current app user
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId);

            if (appUser == null)
                return Forbid();

            // Create folder
            var uploadsRoot = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "requests",
                RequestId.Value.ToString());

            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await Upload.CopyToAsync(stream);
            }

            var row = new attachments
            {
                requestID = RequestId.Value,
                creatorID = appUser.userID,
                fileName = Upload.FileName,
                contentType = Upload.ContentType,
                fileUrl = $"/uploads/requests/{RequestId.Value}/{safeFileName}",
                uploadedAt = DateTime.UtcNow
            };

            _context.attachments.Add(row);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Attachment uploaded. FileID={FileID}, RequestId={RequestId}",
                row.fileID,
                row.requestID);

            return RedirectToPage("/RequestPages/Details",
                new { id = RequestId.Value });
        }

        private async Task<bool> CanAccessRequestAsync(int requestId)
        {
            if (User.IsInRole("Admin") ||
                User.IsInRole("Manager") ||
                User.IsInRole("Staff"))
            {
                return await _context.request
                    .AnyAsync(r => r.requestID == requestId);
            }

            if (User.IsInRole("User"))
            {
                var identityUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                var appUser = await _context.users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.identityUserId == identityUserId);

                if (appUser == null) return false;

                return await _context.request
                    .AnyAsync(r =>
                        r.requestID == requestId &&
                        r.created_by == appUser.userID);
            }

            return false;
        }
    }
}