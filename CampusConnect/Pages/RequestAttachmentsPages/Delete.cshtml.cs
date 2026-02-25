using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestAttachmentsPages
{
    [Authorize(Roles = "Admin,Manager")]
    public class DeleteModel : PageModel
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(TablesDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Attachments attachments { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var found = await _context.attachments.FirstOrDefaultAsync(m => m.fileID == id);
            if (found == null)
                return NotFound();

            // Assign to the PageModel property so the Razor page can display it
            attachments = found;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var found = await _context.attachments.FindAsync(id);
            if (found != null)
            {
                _context.attachments.Remove(found);
                await _context.SaveChangesAsync();

                // ⭐ CRITICAL OPERATION LOGGING
                _logger.LogWarning(
                    "CRITICAL: Attachment deleted. FileId={FileId} RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    found.fileID,
                    found.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }

            return RedirectToPage("./Index");
        }
    }
}