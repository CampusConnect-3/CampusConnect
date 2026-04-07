using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CampusConnect.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TestMongoModel : PageModel
    {
        private readonly MongoDBService _mongoService;
        private readonly RequestSyncService _syncService;
        private readonly GeminiInsightService _geminiService;

        public TestMongoModel(
            MongoDBService mongoService,
            RequestSyncService syncService,
            GeminiInsightService geminiService)
        {
            _mongoService = mongoService;
            _syncService = syncService;
            _geminiService = geminiService;
        }

        public int SyncedCount { get; set; }
        public int InsightCount { get; set; }
        public long ActivityLogCount { get; set; }
        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            var snapshots = await _mongoService.RequestSnapshots.CountDocumentsAsync(FilterDefinition<RequestSnapshot>.Empty);
            var insights = await _mongoService.AIInsights.CountDocumentsAsync(FilterDefinition<AIInsight>.Empty);
            var activityLogs = await _mongoService.GetActivityLogCountAsync();

            SyncedCount = (int)snapshots;
            InsightCount = (int)insights;
            ActivityLogCount = activityLogs;
        }

        public async Task<IActionResult> OnPostSyncAllAsync()
        {
            await _syncService.SyncAllRequestsAsync();
            Message = "✅ All requests synced to MongoDB!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGenerateInsightsAsync()
        {
            try
            {
                await _geminiService.GenerateBuildingInsightsAsync();
                Message = "✅ Generated building insights successfully!";
            }
            catch (Exception ex)
            {
                Message = $"⚠️ Failed to generate insights: {ex.Message}";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSeedActivityLogsAsync()
        {
            var sampleLogs = new[]
            {
                new ActivityLog
                {
                    UserId = "sample-user-1",
                    UserName = "Kate Eggenberg",
                    UserRole = "Staff",
                    Action = "created_request",
                    RequestId = 1018,
                    RequestTitle = "Walking leaking form toilet",
                    IpAddress = "192.168.1.100",
                    Details = new BsonDocument
                    {
                        { "priority", "Critical" },
                        { "building", "Oak Hall" },
                        { "room", "315" }
                    }
                },
                new ActivityLog
                {
                    UserId = "sample-user-1",
                    UserName = "Kate Eggenberg",
                    UserRole = "Staff",
                    Action = "assigned_request",
                    RequestId = 1027,
                    RequestTitle = "Shower Not Heating",
                    IpAddress = "192.168.1.100",
                    Details = new BsonDocument
                    {
                        { "assignedTo", "Kate Eggenberg" },
                        { "department", "Plumbing" }
                    }
                },
                new ActivityLog
                {
                    UserId = "sample-user-2",
                    UserName = "John Manager",
                    UserRole = "Manager",
                    Action = "status_changed",
                    RequestId = 1025,
                    RequestTitle = "Leaking Pipe Under Sink",
                    IpAddress = "192.168.1.101",
                    Details = new BsonDocument
                    {
                        { "oldStatus", "To-Do" },
                        { "newStatus", "In Progress" }
                    }
                },
                new ActivityLog
                {
                    UserId = "sample-user-1",
                    UserName = "Kate Eggenberg",
                    UserRole = "Staff",
                    Action = "added_comment",
                    RequestId = 1018,
                    RequestTitle = "Walking leaking form toilet",
                    IpAddress = "192.168.1.100",
                    Details = new BsonDocument
                    {
                        { "commentText", "Working on this now, should be fixed by end of day" }
                    }
                },
                new ActivityLog
                {
                    UserId = "sample-user-3",
                    UserName = "Admin User",
                    UserRole = "Admin",
                    Action = "reviewed_logs",
                    IpAddress = "192.168.1.102",
                    Details = new BsonDocument
                    {
                        { "logsReviewed", 25 },
                        { "timeSpent", "15 minutes" }
                    }
                },
                new ActivityLog
                {
                    UserId = "sample-user-2",
                    UserName = "John Manager",
                    UserRole = "Manager",
                    Action = "status_changed",
                    RequestId = 1023,
                    RequestTitle = "Bathroom Sink Not Draining",
                    IpAddress = "192.168.1.101",
                    Details = new BsonDocument
                    {
                        { "oldStatus", "In Progress" },
                        { "newStatus", "Completed" }
                    }
                },
                new ActivityLog
                {
                    UserId = "sample-user-1",
                    UserName = "Kate Eggenberg",
                    UserRole = "Staff",
                    Action = "created_request",
                    RequestId = 1003,
                    RequestTitle = "Clogged Toilet",
                    IpAddress = "192.168.1.100",
                    Details = new BsonDocument
                    {
                        { "priority", "Low" },
                        { "building", "Gerard Hall" },
                        { "room", "301" }
                    }
                }
            };

            foreach (var log in sampleLogs)
            {
                await _mongoService.LogActivityAsync(log);
            }

            Message = $"✅ Seeded {sampleLogs.Length} sample activity logs to MongoDB!";
            return RedirectToPage();
        }
    }
}