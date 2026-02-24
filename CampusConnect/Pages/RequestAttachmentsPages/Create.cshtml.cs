using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CampusConnect.Pages.RequestAttachmentsPages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        private readonly ILogger<CreateModel> _logger;
        public CreateModel(TablesDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
        ViewData["creatorID"] = new SelectList(_context.users, "userID", "email");
        ViewData["requestID"] = new SelectList(_context.request, "requestID", "buildingName");
            return Page();
        }

        [BindProperty]
        public attachments attachments { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.attachments.Add(attachments);
            await _context.SaveChangesAsync();

            _logger.LogInformation("CRITICAL: Attachment uploaded. AttachmentId={AttachmentId} UserId={UserId} TraceId={TraceId}",
                attachments.fileID, attachments.requestID, User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                HttpContext.TraceIdentifier 
             );

            return RedirectToPage("./Index");
        }
    }
}
