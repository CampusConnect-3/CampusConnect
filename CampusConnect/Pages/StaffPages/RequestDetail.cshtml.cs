using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CampusConnect.Pages.StaffPages
{
    [Authorize(Roles = nameof(Roles.Staff))]
    public class RequestDetailModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RequestDetailModel(TablesDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public request Request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int requestId)
        {
            Request = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.comments)
                    .ThenInclude(c => c.creator)
                .Include(r => r.attachments)
                    .ThenInclude(a => a.creator)
                .FirstOrDefaultAsync(r => r.requestID == requestId);

            if (Request == null)
            {
                return NotFound();
            }

            return Partial("_RequestDetail", Request);
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int requestId, string commentText)
        {
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
                commentText = commentText,
                createdAt = DateTime.Now,
                creatorID = currentUser!.userID
            };

            _context.requestComments.Add(comment);
            await _context.SaveChangesAsync();

            // TODO: Trigger notification to request creator

            return RedirectToPage(new { requestId });
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

            return RedirectToPage(new { requestId });
        }
    }
}