using CampusConnect.Data;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Pages.Staff
{
    [Authorize(Roles = "Staff,Admin")]
    public class MyAnalyticsModel : PageModel
    {
        private readonly AnalyticsService _analyticsService;
        private readonly TablesDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyAnalyticsModel(
            AnalyticsService analyticsService, 
            TablesDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _analyticsService = analyticsService;
            _context = context;
            _userManager = userManager;
        }

        public StaffAnalyticsData DashboardData { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get current user's identity
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Find the corresponding user in the users table
            var currentUser = await _context.users
                .FirstOrDefaultAsync(u => u.identityUserId == identityUser.Id);

            if (currentUser == null)
            {
                return NotFound("User profile not found.");
            }

            DashboardData = await _analyticsService.GetStaffDashboardDataAsync(
                currentUser.userID, 
                StartDate, 
                EndDate);

            return Page();
        }
    }
}