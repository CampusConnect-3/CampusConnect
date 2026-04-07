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
        public string Id { get; set; } = string.Empty;

        public ActivityLog? ActivityLog { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var logs = await _mongoService.GetRecentActivityAsync(1000);
            ActivityLog = logs.FirstOrDefault(l => l.Id == id);

            if (ActivityLog == null) return NotFound();

            Id = id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var success = await _mongoService.DeleteActivityLogAsync(Id);
            if (!success) return NotFound();

            return RedirectToPage("./Index");
        }
    }
}