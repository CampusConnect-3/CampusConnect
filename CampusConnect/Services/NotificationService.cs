using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountForIdentityUserAsync(string identityUserId, CancellationToken cancellationToken = default);
        Task<List<notification>> GetRecentForIdentityUserAsync(string identityUserId, int take = 5, CancellationToken cancellationToken = default);
        Task<List<notification>> GetAllForIdentityUserAsync(string identityUserId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(int notificationId, string identityUserId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(string identityUserId, CancellationToken cancellationToken = default);
        Task CreateStatusChangedNotificationAsync(int requestId, string recipientIdentityUserId, int? statusId, CancellationToken cancellationToken = default);
        Task CreateStudentAssignmentNotificationAsync(int requestId, string recipientIdentityUserId, int assignedToUserId, CancellationToken cancellationToken = default);
        Task CreateStaffAssignmentNotificationAsync(int requestId, string recipientIdentityUserId, string requesterName, CancellationToken cancellationToken = default);
        Task CreateStaffCommentNotificationAsync(int requestId, string recipientIdentityUserId, int actorUserId, string commentText, CancellationToken cancellationToken = default);
        Task CreateStudentCommentNotificationAsync(int requestId, string recipientIdentityUserId, string studentName, string commentText, CancellationToken cancellationToken = default);
    }

    public class NotificationService : INotificationService
    {
        private readonly TablesDbContext _context;

        public NotificationService(TablesDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetUnreadCountForIdentityUserAsync(string identityUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
                return 0;

            return await _context.notifications
                .AsNoTracking()
                .CountAsync(n => n.userId == identityUserId && !n.isRead, cancellationToken);
        }

        public async Task<List<notification>> GetRecentForIdentityUserAsync(string identityUserId, int take = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
                return new List<notification>();

            return await _context.notifications
                .AsNoTracking()
                .Where(n => n.userId == identityUserId)
                .OrderByDescending(n => n.createdAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<notification>> GetAllForIdentityUserAsync(string identityUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
                return new List<notification>();

            return await _context.notifications
                .AsNoTracking()
                .Where(n => n.userId == identityUserId)
                .OrderByDescending(n => n.createdAt)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAsReadAsync(int notificationId, string identityUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
                return;

            var notification = await _context.notifications
                .FirstOrDefaultAsync(n => n.id == notificationId && n.userId == identityUserId, cancellationToken);

            if (notification == null || notification.isRead)
                return;

            notification.isRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(string identityUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
                return;

            var notifications = await _context.notifications
                .Where(n => n.userId == identityUserId && !n.isRead)
                .ToListAsync(cancellationToken);

            if (!notifications.Any())
                return;

            foreach (var notification in notifications)
            {
                notification.isRead = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateStatusChangedNotificationAsync(int requestId, string recipientIdentityUserId, int? statusId, CancellationToken cancellationToken = default)
        {
            var statusName = await _context.requestStatus
                .AsNoTracking()
                .Where(s => s.statusID == statusId)
                .Select(s => s.statusName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Updated";

            await CreateNotificationAsync(
                recipientIdentityUserId,
                requestId,
                "Ticket status updated",
                $"Your ticket status is now {statusName}.",
                primaryActionText: "View ticket",
                primaryActionUrl: $"/RequestPages/Details?id={requestId}",
                cancellationToken: cancellationToken);
        }

        public async Task CreateStudentAssignmentNotificationAsync(int requestId, string recipientIdentityUserId, int assignedToUserId, CancellationToken cancellationToken = default)
        {
            var assignee = await _context.users
                .AsNoTracking()
                .Where(u => u.userID == assignedToUserId)
                .Select(u => new
                {
                    Name = ((u.fName + " " + u.lName).Trim()) == "" ? u.email : (u.fName + " " + u.lName).Trim(),
                    Role = u.department
                })
                .FirstOrDefaultAsync(cancellationToken);

            var assigneeName = assignee?.Name ?? "a staff member";

            await CreateNotificationAsync(
                recipientIdentityUserId,
                requestId,
                "Ticket assigned",
                $"Your ticket has been assigned to {assigneeName}.",
                assigneeName: assigneeName,
                assigneeRole: assignee?.Role,
                primaryActionText: "View ticket",
                primaryActionUrl: $"/RequestPages/Details?id={requestId}",
                cancellationToken: cancellationToken);
        }

        public async Task CreateStaffAssignmentNotificationAsync(int requestId, string recipientIdentityUserId, string requesterName, CancellationToken cancellationToken = default)
        {
            await CreateNotificationAsync(
                recipientIdentityUserId,
                requestId,
                "Ticket assigned to you",
                $"{requesterName} has a ticket that is now assigned to you.",
                primaryActionText: "Open ticket",
                primaryActionUrl: $"/RequestPages/Details?id={requestId}",
                cancellationToken: cancellationToken);
        }

        public async Task CreateStaffCommentNotificationAsync(int requestId, string recipientIdentityUserId, int actorUserId, string commentText, CancellationToken cancellationToken = default)
        {
            var actorName = await _context.users
                .AsNoTracking()
                .Where(u => u.userID == actorUserId)
                .Select(u => ((u.fName + " " + u.lName).Trim()) == "" ? u.email : (u.fName + " " + u.lName).Trim())
                .FirstOrDefaultAsync(cancellationToken) ?? "A staff member";

            var preview = string.IsNullOrWhiteSpace(commentText)
                ? $"{actorName} commented on your ticket."
                : $"{actorName} commented: {TrimPreview(commentText)}";

            await CreateNotificationAsync(
                recipientIdentityUserId,
                requestId,
                "New staff comment",
                preview,
                primaryActionText: "View comment",
                primaryActionUrl: $"/RequestPages/Details?id={requestId}",
                cancellationToken: cancellationToken);
        }

        public async Task CreateStudentCommentNotificationAsync(int requestId, string recipientIdentityUserId, string studentName, string commentText, CancellationToken cancellationToken = default)
        {
            var preview = string.IsNullOrWhiteSpace(commentText)
                ? $"{studentName} commented on an assigned ticket."
                : $"{studentName} commented: {TrimPreview(commentText)}";

            await CreateNotificationAsync(
                recipientIdentityUserId,
                requestId,
                "New student comment",
                preview,
                primaryActionText: "View comment",
                primaryActionUrl: $"/RequestPages/Details?id={requestId}",
                cancellationToken: cancellationToken);
        }

        private async Task CreateNotificationAsync(
            string recipientIdentityUserId,
            int requestId,
            string title,
            string message,
            string? assigneeName = null,
            string? assigneeRole = null,
            string? primaryActionText = null,
            string? primaryActionUrl = null,
            CancellationToken cancellationToken = default)
        {
            var notification = new notification
            {
                userId = recipientIdentityUserId,
                requestId = requestId,
                title = title,
                message = message,
                isRead = false,
                createdAt = DateTimeOffset.UtcNow,
                assigneeName = assigneeName,
                assigneeRole = assigneeRole,
                primaryActionText = primaryActionText,
                primaryActionUrl = primaryActionUrl
            };

            _context.notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string TrimPreview(string value)
        {
            var trimmed = value.Trim();
            return trimmed.Length <= 120 ? trimmed : trimmed[..117] + "...";
        }
    }
}
