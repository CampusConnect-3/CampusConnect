using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CampusConnect.Pages.Notifications
{
    [Authorize(Roles = "User,Staff,Manager,Admin")]
    public class IndexModel : PageModel
    {
        private readonly INotificationService _notifications;

        public IndexModel(INotificationService notifications)
        {
            _notifications = notifications;
        }

        public List<notification> Notifications { get; private set; } = new();
        public int UnreadCount { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
                return Forbid();

            Notifications = await _notifications.GetAllForIdentityUserAsync(identityUserId, cancellationToken);
            UnreadCount = await _notifications.GetUnreadCountForIdentityUserAsync(identityUserId, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync(CancellationToken cancellationToken = default)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
                return Forbid();

            await _notifications.MarkAllAsReadAsync(identityUserId, cancellationToken);
            return RedirectToPage();
        }
    }
}
