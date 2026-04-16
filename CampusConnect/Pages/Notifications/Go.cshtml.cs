using CampusConnect.Data;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusConnect.Pages.Notifications
{
    [Authorize(Roles = "User,Staff,Manager,Admin")]
    public class GoModel : PageModel
    {
        private readonly INotificationService _notifications;
        private readonly TablesDbContext _context;

        public GoModel(INotificationService notifications, TablesDbContext context)
        {
            _notifications = notifications;
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int notificationId, CancellationToken cancellationToken = default)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
                return Forbid();

            var notification = await _context.notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.id == notificationId && n.userId == identityUserId, cancellationToken);

            if (notification == null)
                return RedirectToPage("/Notifications/Index");

            await _notifications.MarkAsReadAsync(notificationId, identityUserId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(notification.primaryActionUrl) && Url.IsLocalUrl(notification.primaryActionUrl))
                return LocalRedirect(notification.primaryActionUrl);

            return RedirectToPage("/RequestPages/Details", new { id = notification.requestId });
        }
    }
}
