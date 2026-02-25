using System;
using System.Threading.Tasks;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CampusConnect.Pages.RequestPages
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

        [BindProperty]
        public Request request { get; set; } = default!;

        public IActionResult OnGet()
        {
            PopulateDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return Page();
            }

            // Resolve the currently authenticated user (Identity)
            var currentUserName = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUserName))
            {
                ModelState.AddModelError(string.Empty, "Unable to determine the current user. Please sign in again.");
                PopulateDropdowns();
                return Page();
            }

            // Match Identity name to legacy users table row
            var currentUser = await _context.users.FirstOrDefaultAsync(u => u.email == currentUserName);
            if (currentUser == null)
            {
                ModelState.AddModelError(string.Empty, "Your account is not present in the application's users table. Contact an administrator.");
                PopulateDropdowns();
                return Page();
            }

            // Set FK + timestamps server-side (prevents tampering)
            request.created_by = currentUser.userID;
            request.createdAt = DateTime.Now; // use DateTime.UtcNow if you prefer UTC

            _context.request.Add(request);
            await _context.SaveChangesAsync();

            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "CRITICAL: Request created. RequestId={RequestId} CreatedBy={CreatedBy} UserId={UserId} TraceId={TraceId}",
                request.requestID,
                request.created_by,
                userId ?? "Unknown",
                HttpContext.TraceIdentifier
            );

            return RedirectToPage("./Index");
        }

        private void PopulateDropdowns()
        {
            ViewData["assigned_to"] = new SelectList(_context.users, "userID", "email");
            ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
            ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
            ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");

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