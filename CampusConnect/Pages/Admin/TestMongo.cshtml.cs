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
    }
}