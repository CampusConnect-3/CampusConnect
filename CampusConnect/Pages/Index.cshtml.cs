using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;

namespace CampusConnect.Pages.Home
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        public string DisplayName { get; private set; } = "User";
        public string StudentId { get; private set; } = "";
        public int OpenCount { get; private set; } = 0;
        public int InProgressCount { get; private set; } = 0;
        public int ClosedCount { get; private set; } = 0;

        public List<(string Id, string Title, string Status, DateTime LastUpdated)> RecentRequests { get; private set; }
            = new();

        public IndexModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Redirect admins to admin home
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Admin/Index");
            }

            DisplayName = user.UserName ?? user.Email ?? "User";

            // StudentId and counts / recent list: populate from your existing CRUD/data stores.
            // Here we populate some safe defaults for immediate compile/run.
            StudentId = User.FindFirst("StudentId")?.Value ?? "N/A";

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