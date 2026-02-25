using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestAttachmentsPages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly TablesDbContext _context;
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
        public Attachments attachments { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            _context.attachments.Add(attachments);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "CRITICAL: Attachment uploaded. AttachmentId={AttachmentId} RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                attachments.fileID,
                attachments.requestID,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                HttpContext.TraceIdentifier
            );

            return RedirectToPage("./Index");
        }
    }
}