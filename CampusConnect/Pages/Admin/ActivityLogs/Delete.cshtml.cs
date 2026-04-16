using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages.Admin.ActivityLogs
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly MongoDBService _mongoService;

        public DeleteModel(MongoDBService mongoService)
        {
            _mongoService = mongoService;
        }

        [BindProperty]
        public ActivityLog ActivityLog { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var activityLog = await _mongoService.GetActivityByIdAsync(id);

            if (activityLog == null)
            {
                return NotFound();
            }

            ActivityLog = activityLog;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var success = await _mongoService.DeleteActivityLogAsync(id);

            if (!success)
            {
                return NotFound();
            }

            return RedirectToPage("./Index");
        }
    }
}