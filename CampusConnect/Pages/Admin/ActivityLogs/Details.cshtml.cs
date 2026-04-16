using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages.Admin.ActivityLogs
{
    [Authorize(Roles = "Admin,Manager")]
    public class DetailsModel : PageModel
    {
        private readonly MongoDBService _mongoService;

        public DetailsModel(MongoDBService mongoService)
        {
            _mongoService = mongoService;
        }

        public ActivityLog? ActivityLog { get; set; }
        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            ActivityLog = await _mongoService.GetActivityByIdAsync(id);

            if (ActivityLog == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostMarkReviewedAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var reviewedBy = User.Identity?.Name ?? "Admin";
            var success = await _mongoService.UpdateActivityLogAsync(
                id,
                reviewedBy,
                "Marked as reviewed from details page"
            );

            if (!success)
            {
                TempData["Error"] = "Failed to mark activity log as reviewed.";
                return RedirectToPage(new { id });
            }

            TempData["Success"] = "✅ Activity log marked as reviewed!";
            return RedirectToPage(new { id });
        }
    }
} 