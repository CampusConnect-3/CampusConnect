using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CampusConnect.Pages.RequestAttachmentsPages
{
    public class DeleteModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        private readonly ILogger<DeleteModel> _logger;
        public DeleteModel(TablesDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public attachments attachments { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attachments = await _context.attachments.FirstOrDefaultAsync(m => m.fileID == id);

            if (attachments == null)
            {
                return NotFound();
            }
            else
            {
                attachments = attachments;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attachments = await _context.attachments.FindAsync(id);
            if (attachments != null)
            {
                _context.attachments.Remove(attachments);
                await _context.SaveChangesAsync();

                // ⭐ CRITICAL OPERATION LOGGING
                _logger.LogWarning(
                    "CRITICAL: Attachment deleted. FileId={FileId} RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    attachments.fileID,
                    attachments.requestID,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier
                );
            }

            return RedirectToPage("./Index");
        }
    }
}
