using CampusConnect.Configuration;
using CampusConnect.Models.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CampusConnect.Services;

public class GeminiInsightService
{
    private readonly MongoDBService _mongoService;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiInsightService> _logger;
    private readonly HttpClient _httpClient;

    public GeminiInsightService(
        MongoDBService mongoService,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiInsightService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _mongoService = mongoService;
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task GenerateBuildingInsightsAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Starting building pattern analysis with Gemini...");

            // Validate API key first
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("❌ Gemini API key is not configured. Please add it to User Secrets.");
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentRequests = await _mongoService.RequestSnapshots
                .Find(r => r.CreatedAt >= thirtyDaysAgo)
                .ToListAsync();

            _logger.LogInformation("📊 Found {Count} requests in last 30 days", recentRequests.Count);

            if (!recentRequests.Any())
            {
                _logger.LogWarning("⚠️ No requests found in MongoDB from last 30 days. Make sure to sync requests first.");
                return;
            }

            var buildingGroups = recentRequests
                .GroupBy(r => r.Building.BuildingName)
                .Select(g => new { Building = g.Key, Count = g.Count(), Requests = g.ToList() })
                .ToList();

            _logger.LogInformation("🏢 Found {Count} buildings with requests", buildingGroups.Count);
            foreach (var group in buildingGroups)
            {
                _logger.LogInformation("   • {Building}: {Count} requests", group.Building, group.Count);
            }

            var qualifyingBuildings = buildingGroups.Where(g => g.Count >= 5).ToList();
            _logger.LogInformation("✅ {Count} buildings qualify for analysis (5+ requests)", qualifyingBuildings.Count);

            if (!qualifyingBuildings.Any())
            {
                _logger.LogWarning("⚠️ No buildings have enough requests (need 5+). Current building counts:");
                foreach (var group in buildingGroups)
                {
                    _logger.LogWarning("   • {Building}: {Count}/5 requests", group.Building, group.Count);
                }
                return;
            }

            foreach (var buildingGroup in qualifyingBuildings)
            {
                await AnalyzeBuildingAsync(buildingGroup.Building, buildingGroup.Requests);
            }

            _logger.LogInformation("✅ Building pattern analysis complete. Analyzed {Count} buildings", qualifyingBuildings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating building insights");
            throw;
        }
    }

    private async Task AnalyzeBuildingAsync(string buildingName, List<RequestSnapshot> requests)
    {
        try
        {
            _logger.LogInformation("🔎 Analyzing {Building} with {Count} requests", buildingName, requests.Count);

            var categoryGroups = requests
                .GroupBy(r => r.Category.CategoryName)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / requests.Count * 100,
                    CommonKeywords = g.SelectMany(r => r.ExtractedKeywords ?? new List<string>())
                        .GroupBy(k => k)
                        .OrderByDescending(k => k.Count())
                        .Take(5)
                        .Select(k => k.Key)
                        .ToList(),
                    RecentRequests = g.OrderByDescending(r => r.CreatedAt).Take(5).ToList()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            _logger.LogInformation("   📂 Categories in {Building}:", buildingName);
            foreach (var cat in categoryGroups)
            {
                _logger.LogInformation("      • {Category}: {Count} requests ({Percentage:F1}%)", 
                    cat.Category, cat.Count, cat.Percentage);
            }

            var topCategory = categoryGroups.FirstOrDefault();
            if (topCategory == null)
            {
                _logger.LogWarning("   ⚠️ No categories found for {Building}", buildingName);
                return;
            }

            if (topCategory.Count < 3)
            {
                _logger.LogWarning("   ⚠️ Top category '{Category}' only has {Count}/3 requests. Skipping.", 
                    topCategory.Category, topCategory.Count);
                return;
            }

            _logger.LogInformation("   ✅ Top category '{Category}' has {Count} requests (meets 3+ threshold)", 
                topCategory.Category, topCategory.Count);

            // Check for existing insight
            var existingInsight = await _mongoService.AIInsights
                .Find(i => i.BuildingName == buildingName 
                    && i.CategoryName == topCategory.Category 
                    && i.ResolvedAt == null
                    && i.GeneratedAt >= DateTime.UtcNow.AddDays(-14))
                .FirstOrDefaultAsync();

            if (existingInsight != null)
            {
                _logger.LogInformation("   ⏭️ Similar active insight already exists for {Building} - {Category} (generated {Date})", 
                    buildingName, topCategory.Category, existingInsight.GeneratedAt);
                return;
            }

            _logger.LogInformation("   🤖 Calling Gemini API for analysis...");
            var prompt = BuildAnalysisPrompt(buildingName, requests, topCategory);
            var aiAnalysis = await CallGeminiApiAsync(prompt);

            var severity = DetermineSeverity(topCategory.Count, topCategory.Percentage, requests.Count);

            var insight = new AIInsight
            {
                InsightType = "BuildingPattern",
                Severity = severity,
                BuildingName = buildingName,
                CategoryName = topCategory.Category,
                Title = $"{buildingName}: {topCategory.Category} Pattern Detected",
                Description = aiAnalysis,
                Recommendation = ExtractRecommendation(aiAnalysis),
                Metrics = new InsightMetrics
                {
                    RequestCount = topCategory.Count,
                    DaySpan = 30,
                    IncreasePercentage = topCategory.Percentage,
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "TopKeywords", topCategory.CommonKeywords },
                        { "TotalBuildingRequests", requests.Count }
                    }
                },
                RelatedRequestIds = topCategory.RecentRequests.Select(r => r.RequestId).ToList(),
                GeneratedAt = DateTime.UtcNow,
                AIModel = _settings.Model,
                ConfidenceScore = CalculateConfidence(topCategory.Count, requests.Count)
            };

            await _mongoService.AIInsights.InsertOneAsync(insight);
            _logger.LogInformation("   ✅ Generated {Severity} insight for {Building} - {Category}", 
                severity, buildingName, topCategory.Category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error analyzing building {BuildingName}", buildingName);
        }
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = _settings.MaxTokens,
                temperature = 0.7
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            // NEVER log the API key!
            _logger.LogDebug("Calling Gemini API for model: {Model}", _settings.Model);
            
            var response = await _httpClient.PostAsync(url, content);
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: Status {Status}, Error: {Error}", 
                    response.StatusCode, 
                    responseBody.Length > 200 ? responseBody.Substring(0, 200) + "..." : responseBody);
                throw new Exception($"Gemini API returned {response.StatusCode}");
            }

            var result = JsonDocument.Parse(responseBody);

            return result.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "No response generated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            throw;
        }
    }

    private string BuildAnalysisPrompt(string buildingName, List<RequestSnapshot> allRequests, dynamic topCategory)
    {
        var categoryCount = (int)topCategory.Count;
        var categoryPercentage = (double)topCategory.Percentage;
        var categoryName = (string)topCategory.Category;
        var commonKeywords = (List<string>)topCategory.CommonKeywords;
        var recentRequests = (List<RequestSnapshot>)topCategory.RecentRequests;

        var requestExamples = string.Join("\n",
            recentRequests.Take(3).Select((r, i) =>
                $"{i + 1}. [{r.Priority}] {r.Title} - {r.Description.Substring(0, Math.Min(100, r.Description.Length))}..."));

        return $@"You are a facilities management AI analyzing campus maintenance patterns.

Building: {buildingName}
Category: {categoryName}
Pattern Detected: {categoryCount} requests ({categoryPercentage:F1}% of all building requests) in last 30 days

Common Keywords: {string.Join(", ", commonKeywords)}

Recent Request Examples:
{requestExamples}

Provide a concise analysis (2-3 sentences) that:
1. Identifies the likely root cause
2. Suggests immediate action
3. Recommends preventive measures

Keep it professional and actionable.";
    }

    private string DetermineSeverity(int count, double percentage, int totalRequests)
    {
        if (count >= 10 || percentage >= 60)
            return "Critical";
        if (count >= 6 || percentage >= 40)
            return "High";
        if (count >= 4 || percentage >= 25)
            return "Medium";
        return "Low";
    }

    private string ExtractRecommendation(string aiAnalysis)
    {
        var sentences = aiAnalysis.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var recommendation = sentences.FirstOrDefault(s => 
            s.Contains("recommend", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("suggest", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("should", StringComparison.OrdinalIgnoreCase));
        
        return recommendation?.Trim() ?? sentences.LastOrDefault()?.Trim() ?? "Review and address identified issues.";
    }

    private double CalculateConfidence(int categoryCount, int totalCount)
    {
        var percentage = (double)categoryCount / totalCount;
        
        if (categoryCount >= 8 && percentage >= 0.5)
            return 0.95;
        if (categoryCount >= 5 && percentage >= 0.4)
            return 0.85;
        if (categoryCount >= 4 && percentage >= 0.3)
            return 0.75;
        
        return 0.65;
    }

    // Add this method to GeminiInsightService class
    public async Task<List<string>> ListAvailableModelsAsync()
    {
        try
        {
            _logger.LogInformation("=== GEMINI API DIAGNOSTICS ===");
            _logger.LogInformation("API Key (first 10 chars): {KeyPreview}", _settings.ApiKey?.Substring(0, Math.Min(10, _settings.ApiKey?.Length ?? 0)));
            _logger.LogInformation("API Key Length: {Length}", _settings.ApiKey?.Length ?? 0);
            _logger.LogInformation("Model Setting: {Model}", _settings.Model);
            
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_settings.ApiKey}";
            _logger.LogInformation("Request URL: {Url}", url.Replace(_settings.ApiKey, "***API_KEY***"));
            
            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Response Status: {Status}", response.StatusCode);
            _logger.LogInformation("Response Body: {Body}", responseBody);
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonDocument.Parse(responseBody);
                var models = new List<string>();
                
                if (result.RootElement.TryGetProperty("models", out var modelsArray))
                {
                    foreach (var model in modelsArray.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name))
                        {
                            var modelName = name.GetString();
                            if (modelName != null)
                            {
                                // Extract just the model name (remove "models/" prefix)
                                var cleanName = modelName.Replace("models/", "");
                                models.Add(cleanName);
                                _logger.LogInformation("Found model: {ModelName}", cleanName);
                            }
                        }
                    }
                }
                
                return models;
            }
            else
            {
                _logger.LogError("API request failed with status {Status}: {Body}", response.StatusCode, responseBody);
            }
            
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing available models");
            return new List<string>();
        }
    }
}