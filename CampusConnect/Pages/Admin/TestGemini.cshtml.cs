using CampusConnect.Data;
using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace CampusConnect.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TestGeminiModel : PageModel
    {
        private readonly GeminiInsightService _geminiService;
        private readonly MongoDBService _mongoService;
        private readonly TablesDbContext _dbContext;
        private readonly RequestSyncService _syncService;
        private readonly ILogger<TestGeminiModel> _logger;

        public TestGeminiModel(
            GeminiInsightService geminiService,
            MongoDBService mongoService,
            TablesDbContext dbContext,
            RequestSyncService syncService,
            ILogger<TestGeminiModel> logger)
        {
            _geminiService = geminiService;
            _mongoService = mongoService;
            _dbContext = dbContext;
            _syncService = syncService;
            _logger = logger;
        }

        public List<string> AvailableModels { get; set; } = new();
        public string CurrentModel { get; set; } = string.Empty;
        public bool MongoDbConnected { get; set; }
        public long RequestSnapshotCount { get; set; }
        public long InsightCount { get; set; }
        public string? SyncMessage { get; set; }
        public string? InsightMessage { get; set; }
        public List<AIInsight> RecentInsights { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Check available models
            AvailableModels = await _geminiService.ListAvailableModelsAsync();

            // Check MongoDB connection
            try
            {
                if (_mongoService.RequestSnapshots != null)
                {
                    MongoDbConnected = true;
                    RequestSnapshotCount = await _mongoService.RequestSnapshots.CountDocumentsAsync(_ => true);
                    InsightCount = _mongoService.AIInsights != null 
                        ? await _mongoService.AIInsights.CountDocumentsAsync(_ => true) 
                        : 0;

                    // Load recent insights
                    RecentInsights = _mongoService.AIInsights != null
                        ? await _mongoService.AIInsights
                            .Find(_ => true)
                            .SortByDescending(i => i.GeneratedAt)
                            .Limit(5)
                            .ToListAsync()
                        : new List<AIInsight>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking MongoDB connection");
                MongoDbConnected = false;
            }
        }

        public async Task<IActionResult> OnPostSyncRequestsAsync()
        {
            try
            {
                if (_mongoService.RequestSnapshots == null)
                {
                    SyncMessage = "❌ MongoDB is not available";
                    await OnGetAsync();
                    return Page();
                }

                // Use the existing RequestSyncService which has all the logic
                await _syncService.SyncAllRequestsAsync();

                var count = await _mongoService.RequestSnapshots.CountDocumentsAsync(_ => true);
                SyncMessage = $"✅ Successfully synced {count} requests to MongoDB";
                _logger.LogInformation("Synced requests to MongoDB via sync service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing requests");
                SyncMessage = $"❌ Error syncing requests: {ex.Message}";
            }

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateInsightsAsync()
        {
            try
            {
                await _geminiService.GenerateBuildingInsightsAsync();
                InsightMessage = "✅ Insights generated successfully! Check the results below.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating insights");
                InsightMessage = $"❌ Error generating insights: {ex.Message}";
            }

            await OnGetAsync();
            return Page();
        }
    }
}
