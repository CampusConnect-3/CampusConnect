using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;

namespace CampusConnect.Pages.Admin.ActivityLogs
{
    [Authorize(Roles = "Admin,Manager")]
    public class CreateModel : PageModel
    {
        private readonly MongoDBService _mongoService;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(MongoDBService mongoService, UserManager<IdentityUser> userManager)
        {
            _mongoService = mongoService;
            _userManager = userManager;
        }

        [BindProperty]
        public ActivityLog ActivityLog { get; set; } = new();

        [BindProperty]
        public string DetailsJson { get; set; } = "{}";

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ActivityLog.UserId = user.Id;
            ActivityLog.UserName = user.UserName ?? "Unknown";
            ActivityLog.UserRole = User.IsInRole("Admin") ? "Admin" : "Manager";
            ActivityLog.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                ActivityLog.Details = BsonDocument.Parse(DetailsJson);
            }
            catch
            {
                ActivityLog.Details = new BsonDocument { { "raw", DetailsJson } };
            }

            await _mongoService.LogActivityAsync(ActivityLog);

            return RedirectToPage("./Index");
        }
    }
}