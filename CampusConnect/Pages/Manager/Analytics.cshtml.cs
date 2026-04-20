using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager,Admin")]
    public class AnalyticsModel : PageModel
    {
        private readonly AnalyticsService _analyticsService;

        public AnalyticsModel(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public AnalyticsDashboardData DashboardData { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            DashboardData = await _analyticsService.GetDashboardDataAsync(StartDate, EndDate);
            return Page();
        }
    }
}