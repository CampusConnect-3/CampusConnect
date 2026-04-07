using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages.Admin.ActivityLogs
{
    [Authorize(Roles = "Admin,Manager")]
    public class EditModel : PageModel
    {
        private readonly MongoDBService _mongoService;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(MongoDBService mongoService, UserManager<IdentityUser> userManager)
        {
            _mongoService = mongoService;
            _userManager = userManager;
        }

        [BindProperty]
        public string Id { get; set; } = string.Empty;

        [BindProperty]
        public string ReviewNotes { get; set; } = string.Empty;

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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var success = await _mongoService.UpdateActivityLogAsync(Id, user.UserName ?? "Unknown", ReviewNotes);

            if (!success) return NotFound();

            return RedirectToPage("./Index");
        }
    }
}