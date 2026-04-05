using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages.Admin;

[Authorize(Roles = "Admin")]  // Change this temporarily to see if it helps
public class TestMongoModel : PageModel
{
    private readonly RequestSyncService _syncService;
    private readonly MongoDBService _mongoService;
    private readonly GeminiInsightService _aiService;
    private readonly ILogger<TestMongoModel> _logger;

    public TestMongoModel(
        RequestSyncService syncService, 
        MongoDBService mongoService,
        GeminiInsightService aiService,
        ILogger<TestMongoModel> logger)
    {
        _syncService = syncService;
        _mongoService = mongoService;
        _aiService = aiService;
        _logger = logger;
    }

    public string Message { get; set; } = string.Empty;
    public int SyncedCount { get; set; }
    public int InsightCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadCountsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAllAsync()
    {
        try
        {
            await _syncService.SyncAllRequestsAsync();
            Message = $"✅ Successfully synced all requests!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing requests");
            Message = $"❌ Error: {ex.Message}";
        }

        await LoadCountsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateInsightsAsync()
    {
        try
        {
            _logger.LogInformation("Starting AI insight generation...");
            await _aiService.GenerateBuildingInsightsAsync();
            Message = $"✅ Successfully generated AI insights!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI insights");
            Message = $"❌ Error generating insights: {ex.Message}\n\nStack Trace: {ex.StackTrace}";
        }

        await LoadCountsAsync();
        return Page();
    }

    private async Task LoadCountsAsync()
    {
        try
        {
            SyncedCount = (int)await _mongoService.RequestSnapshots.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<Models.MongoDB.RequestSnapshot>.Empty);
            InsightCount = (int)await _mongoService.AIInsights.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<Models.MongoDB.AIInsight>.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading counts");
            SyncedCount = 0;
            InsightCount = 0;
        }
    }
}