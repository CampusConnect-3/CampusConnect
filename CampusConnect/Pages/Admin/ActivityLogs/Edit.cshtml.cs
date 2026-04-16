using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CampusConnect.Pages.Admin.ActivityLogs
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly MongoDBService _mongoService;

        public EditModel(MongoDBService mongoService)
        {
            _mongoService = mongoService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string Id { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Reviewed By")]
            public string ReviewedBy { get; set; } = string.Empty;

            [Display(Name = "Review Notes")]
            [DataType(DataType.MultilineText)]
            public string? ReviewNotes { get; set; }

            [Display(Name = "Mark as Reviewed")]
            public bool Reviewed { get; set; }
        }

        public ActivityLog? ActivityLog { get; set; }

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

            Input = new InputModel
            {
                Id = ActivityLog.Id!,
                ReviewedBy = ActivityLog.ReviewedBy ?? User.Identity?.Name ?? "Admin",
                ReviewNotes = ActivityLog.ReviewNotes,
                Reviewed = ActivityLog.Reviewed
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ActivityLog = await _mongoService.GetActivityByIdAsync(Input.Id);
                return Page();
            }

            var success = await _mongoService.UpdateActivityLogAsync(
                Input.Id,
                Input.ReviewedBy,
                Input.ReviewNotes ?? string.Empty
            );

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to update activity log.");
                ActivityLog = await _mongoService.GetActivityByIdAsync(Input.Id);
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}