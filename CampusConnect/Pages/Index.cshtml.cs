using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages.Home
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public string DisplayName { get; private set; } = "User";
        public string StudentId { get; private set; } = "";
        public int OpenCount { get; private set; } = 0;
        public int InProgressCount { get; private set; } = 0;
        public int ClosedCount { get; private set; } = 0;
        public bool IsAdmin { get; private set; } = false;

        public List<(string Id, string Title, string Status, DateTime LastUpdated)> RecentRequests { get; private set; }
            = new();

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Check if admin (for displaying admin-specific UI elements)
            IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (IsAdmin)
            {
                return RedirectToPage("/Admin/UserPages/Index"); // Changed from /Admin/Index
            }

            DisplayName = user.UserName ?? user.Email ?? "User";

            // StudentId: use your ApplicationUser property first (recommended)
            // Falls back to claim if you set one elsewhere.
            StudentId = user.SchoolId ?? User.FindFirst("StudentId")?.Value ?? "N/A";

            // Replace with real queries against your data context:
            OpenCount = 2;
            InProgressCount = 1;
            ClosedCount = 4;

            RecentRequests.Add(("12345", "Plumbing", "Closed", new DateTime(2026, 1, 14)));
            RecentRequests.Add(("67890", "Wi-Fi", "In Progress", new DateTime(2026, 1, 20)));
            RecentRequests.Add(("38657", "Heater", "To-Do", new DateTime(2026, 1, 16)));

            return Page();
        }
    }
}