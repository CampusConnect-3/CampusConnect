using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages.Admin.ActivityLogs
{
    [Authorize(Roles = "Admin,Manager")]
    public class IndexModel : PageModel
    {
        private readonly MongoDBService _mongoService;

        public IndexModel(MongoDBService mongoService)
        {
            _mongoService = mongoService;
        }

        public List<ActivityLog>? ActivityLogs { get; set; }

        public async Task OnGetAsync()
        {
            ActivityLogs = await _mongoService.GetRecentActivityAsync(100);
        }
    }
}