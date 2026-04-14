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
            _logger.LogInformation("Starting building pattern analysis with Gemini...");

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentRequests = await _mongoService.RequestSnapshots
                .Find(r => r.CreatedAt >= thirtyDaysAgo)
                .ToListAsync();

            var buildingGroups = recentRequests
                .GroupBy(r => r.Building.BuildingName)
                .Where(g => g.Count() >= 5)
                .ToList();

            foreach (var buildingGroup in buildingGroups)
            {
                await AnalyzeBuildingAsync(buildingGroup.Key, buildingGroup.ToList());
            }

            _logger.LogInformation("Building pattern analysis complete. Analyzed {Count} buildings", buildingGroups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating building insights");
            throw;
        }
    }

    private async Task AnalyzeBuildingAsync(string buildingName, List<RequestSnapshot> requests)
    {
        try
        {
            var categoryGroups = requests
                .GroupBy(r => r.Category.CategoryName)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / requests.Count * 100,
                    CommonKeywords = g.SelectMany(r => r.ExtractedKeywords).GroupBy(k => k).OrderByDescending(k => k.Count()).Take(5).Select(k => k.Key).ToList(),
                    RecentRequests = g.OrderByDescending(r => r.CreatedAt).Take(5).ToList()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var topCategory = categoryGroups.FirstOrDefault();
            if (topCategory == null || topCategory.Count < 3)
            {
                return;
            }

            var prompt = BuildAnalysisPrompt(buildingName, requests, topCategory);
            var aiAnalysis = await CallGeminiApiAsync(prompt);

            var severity = DetermineSeverity(topCategory.Count, topCategory.Percentage, requests.Count);

            var existingInsight = await _mongoService.AIInsights
                .Find(i => i.BuildingName == buildingName 
                    && i.CategoryName == topCategory.Category 
                    && i.ResolvedAt == null
                    && i.GeneratedAt >= DateTime.UtcNow.AddDays(-14))
                .FirstOrDefaultAsync();

            if (existingInsight != null)
            {
                _logger.LogInformation("Similar active insight already exists for {Building} - {Category}", buildingName, topCategory.Category);
                return;
            }

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
            _logger.LogInformation("Generated AI insight for {Building} - {Category}: {Severity}", buildingName, topCategory.Category, severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing building {BuildingName}", buildingName);
        }
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        // Use the model name directly (it will be "gemini-2.5-flash")
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
            _logger.LogInformation("Calling Gemini API: {Url}", url.Replace(_settings.ApiKey, "***"));
            var response = await _httpClient.PostAsync(url, content);
            
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini Response Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
            
            response.EnsureSuccessStatusCode();

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

        return $@"Analyze the following facilities maintenance pattern:

**Building:** {buildingName}
**Analysis Period:** Last 30 days
**Total Requests:** {allRequests.Count}

**Top Issue Category:** {categoryName}
- Request Count: {categoryCount} ({categoryPercentage:F1}% of all requests)
- Common Keywords: {string.Join(", ", commonKeywords)}

**Recent Request Examples:**
{requestExamples}

**Question:** Based on this pattern, what is the likely root cause and what specific actions should facility managers take?

Provide your analysis in this format:
1. **Pattern Analysis:** What does this pattern indicate?
2. **Root Cause:** What is the most likely underlying issue?
3. **Recommended Actions:** What specific steps should be taken immediately?
4. **Preventive Measures:** How can this be prevented in the future?

Keep your response concise (under 300 words) and actionable.";
    }

    private string ExtractRecommendation(string aiAnalysis)
    {
        var lines = aiAnalysis.Split('\n');
        var recSection = lines.SkipWhile(l => !l.Contains("Recommended Actions")).Skip(1).TakeWhile(l => !l.StartsWith("**")).ToList();

        if (recSection.Any())
        {
            return string.Join(" ", recSection).Trim();
        }

        return aiAnalysis.Split('\n').FirstOrDefault(l => l.Length > 20)?.Trim() ?? "Review the pattern and take appropriate action.";
    }

    private string DetermineSeverity(int count, double percentage, int totalRequests)
    {
        if (count >= 10 && percentage >= 50)
            return "Critical";
        else if (count >= 7 && percentage >= 30)
            return "Warning";
        else
            return "Info";
    }

    private double CalculateConfidence(int categoryCount, int totalRequests)
    {
        var sampleScore = Math.Min(categoryCount / 10.0, 1.0);
        var concentrationScore = categoryCount / (double)totalRequests;
        return (sampleScore + concentrationScore) / 2.0;
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