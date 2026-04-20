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
        private readonly TestDataSeeder _testDataSeeder;
        private readonly ILogger<TestMongoModel> _logger;

        public TestMongoModel(
            MongoDBService mongoService,
            RequestSyncService syncService,
            GeminiInsightService geminiService,
            TestDataSeeder testDataSeeder,
            ILogger<TestMongoModel> logger)
        {
            _mongoService = mongoService;
            _syncService = syncService;
            _geminiService = geminiService;
            _testDataSeeder = testDataSeeder;
            _logger = logger;
        }

        public int SyncedCount { get; set; }
        public int InsightCount { get; set; }
        public long ActivityLogCount { get; set; }
        public string? Message { get; set; }
        public string? DebugInfo { get; set; }

        public async Task OnGetAsync()
        {
            var snapshots = await _mongoService.RequestSnapshots.CountDocumentsAsync(FilterDefinition<RequestSnapshot>.Empty);
            var insights = await _mongoService.AIInsights.CountDocumentsAsync(FilterDefinition<AIInsight>.Empty);
            var activityLogs = await _mongoService.GetActivityLogCountAsync();

            SyncedCount = (int)snapshots;
            InsightCount = (int)insights;
            ActivityLogCount = activityLogs;

            // Get debug info about patterns
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentRequests = await _mongoService.RequestSnapshots
                .Find(r => r.CreatedAt >= thirtyDaysAgo)
                .ToListAsync();

            var buildingGroups = recentRequests
                .GroupBy(r => r.Building.BuildingName)
                .Select(g => new
                {
                    Building = g.Key,
                    TotalRequests = g.Count(),
                    Categories = g.GroupBy(r => r.Category.CategoryName)
                        .Select(c => new { Category = c.Key, Count = c.Count() })
                        .OrderByDescending(c => c.Count)
                        .ToList()
                })
                .Where(g => g.TotalRequests >= 5)
                .OrderByDescending(g => g.TotalRequests)
                .ToList();

            if (buildingGroups.Any())
            {
                DebugInfo = "Pattern Detection:\n" + string.Join("\n", buildingGroups.Select(b =>
                    $"🏢 {b.Building}: {b.TotalRequests} requests\n" +
                    string.Join("\n", b.Categories.Select(c => $"   • {c.Category}: {c.Count} requests"))
                ));
            }
            else
            {
                DebugInfo = "⚠️ No patterns detected. Buildings need 5+ requests in last 30 days.\n" +
                           $"Current MongoDB snapshots: {recentRequests.Count}";
            }
        }

        public async Task<IActionResult> OnPostSyncAllAsync()
        {
            try
            {
                await _syncService.SyncAllRequestsAsync();
                Message = "✅ All requests synced to MongoDB!";
                _logger.LogInformation("Successfully synced all requests to MongoDB");
            }
            catch (Exception ex)
            {
                Message = $"❌ Sync failed: {ex.Message}";
                _logger.LogError(ex, "Failed to sync requests");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGenerateInsightsAsync()
        {
            try
            {
                // Check if we have enough data first
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var recentCount = await _mongoService.RequestSnapshots
                    .CountDocumentsAsync(r => r.CreatedAt >= thirtyDaysAgo);

                if (recentCount == 0)
                {
                    Message = "⚠️ No recent requests found in MongoDB. Click 'Sync All Requests' first!";
                    return RedirectToPage();
                }

                await _geminiService.GenerateBuildingInsightsAsync();
                
                var newInsightCount = await _mongoService.AIInsights.CountDocumentsAsync(FilterDefinition<AIInsight>.Empty);
                Message = $"✅ Insight generation complete! Total insights: {newInsightCount}";
                _logger.LogInformation("Successfully generated building insights");
            }
            catch (Exception ex)
            {
                Message = $"⚠️ Failed to generate insights: {ex.Message}";
                _logger.LogError(ex, "Failed to generate insights");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSeedTestDataAsync()
        {
            var result = await _testDataSeeder.SeedInsightTriggersAsync();
            Message = result;
            return RedirectToPage();
        }
    }
}