using CampusConnect.Models.MongoDB;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;

namespace CampusConnect.Services
{
    public interface IActivityLoggerService
    {
        Task LogActivityAsync(string action, int? requestId = null, string? requestTitle = null, Dictionary<string, object>? details = null);
    }

    public class ActivityLoggerService : IActivityLoggerService
    {
        private readonly MongoDBService _mongoService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ActivityLoggerService> _logger;

        public ActivityLoggerService(
            MongoDBService mongoService,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ActivityLoggerService> logger)
        {
            _mongoService = mongoService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActivityAsync(string action, int? requestId = null, string? requestTitle = null, Dictionary<string, object>? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    return; // Don't log anonymous actions
                }

                var identityUser = await _userManager.GetUserAsync(httpContext.User);
                if (identityUser == null) return;

                var userRole = GetUserRole(httpContext.User);

                var activityLog = new ActivityLog
                {
                    UserId = identityUser.Id,
                    UserName = identityUser.UserName ?? "Unknown",
                    UserRole = userRole,
                    Action = action,
                    RequestId = requestId,
                    RequestTitle = requestTitle,
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    Details = details != null ? new BsonDocument(details) : new BsonDocument()
                };

                await _mongoService.LogActivityAsync(activityLog);
                _logger.LogInformation("Activity logged: {Action} by {User}", action, identityUser.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log activity: {Action}", action);
                // Don't throw - logging failures shouldn't break the app
            }
        }

        private string GetUserRole(System.Security.Claims.ClaimsPrincipal user)
        {
            if (user.IsInRole("Admin")) return "Admin";
            if (user.IsInRole("Manager")) return "Manager";
            if (user.IsInRole("Staff")) return "Staff";
            if (user.IsInRole("User")) return "User";
            return "Unknown";
        }
    }
}