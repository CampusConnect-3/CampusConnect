using Azure.AI.OpenAI;
using CampusConnect.Configuration;
using CampusConnect.Models.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenAI.Chat;
using System.ClientModel;

namespace CampusConnect.Services;

public class AIInsightService
{
    private readonly MongoDBService _mongoService;
    private readonly ChatClient _chatClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<AIInsightService> _logger;

    public AIInsightService(
        MongoDBService mongoService,
        IOptions<OpenAISettings> settings,
        ILogger<AIInsightService> logger)
    {
        _mongoService = mongoService;
        _settings = settings.Value;
        _logger = logger;

        // Initialize OpenAI client
        _chatClient = new ChatClient(
            model: _settings.Model,
            credential: new ApiKeyCredential(_settings.ApiKey));
    }

    /// <summary>
    /// Analyzes building patterns and generates AI insights
    /// </summary>
    public async Task GenerateBuildingInsightsAsync()
    {
        try
        {
            _logger.LogInformation("Starting building pattern analysis...");

            // Get all request snapshots from last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentRequests = await _mongoService.RequestSnapshots
                .Find(r => r.CreatedAt >= thirtyDaysAgo)
                .ToListAsync();

            // Group by building
            var buildingGroups = recentRequests
                .GroupBy(r => r.Building.BuildingName)
                .Where(g => g.Count() >= 5) // Only analyze buildings with 5+ requests
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

    /// <summary>
    /// Analyzes a specific building's requests using OpenAI
    /// </summary>
    private async Task AnalyzeBuildingAsync(string buildingName, List<RequestSnapshot> requests)
    {
        try
        {
            // Group by category to find patterns
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

            // Check if there's a significant pattern worth analyzing
            var topCategory = categoryGroups.FirstOrDefault();
            if (topCategory == null || topCategory.Count < 3)
            {
                return; // Not enough data for meaningful insight
            }

            // Build context for OpenAI
            var analysisContext = BuildAnalysisPrompt(buildingName, requests, categoryGroups, topCategory);

            // Call OpenAI
            var response = await _chatClient.CompleteChatAsync(
                new List<ChatMessage>
                {
                    new SystemChatMessage("You are a facilities management AI assistant that analyzes building maintenance patterns and provides actionable insights to facility managers. Be specific, data-driven, and focus on root causes and preventive actions."),
                    new UserChatMessage(analysisContext)
                });

            var aiAnalysis = response.Value.Content[0].Text;

            // Determine severity
            var severity = DetermineSeverity(topCategory.Count, topCategory.Percentage, requests.Count);

            // Check if similar insight already exists
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

            // Create new insight
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

    private string BuildAnalysisPrompt(string buildingName, List<RequestSnapshot> allRequests, dynamic categoryGroups, dynamic topCategory)
    {
        // Extract data from dynamic object
        var categoryCount = (int)topCategory.Count;
        var categoryPercentage = (double)topCategory.Percentage;
        var categoryName = (string)topCategory.Category;
        var commonKeywords = (List<string>)topCategory.CommonKeywords;
        var recentRequests = (List<RequestSnapshot>)topCategory.RecentRequests;

        var requestExamples = string.Join("\n", 
            recentRequests.Take(3).Select((r, i) => 
                $"{i + 1}. [{r.Priority}] {r.Title} - {r.Description.Substring(0, Math.Min(100, r.Description.Length))}..."));

        var prompt = $@"Analyze the following facilities maintenance pattern:

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

        return prompt;
    }

    private string ExtractRecommendation(string aiAnalysis)
    {
        // Extract the "Recommended Actions" section
        var lines = aiAnalysis.Split('\n');
        var recSection = lines.SkipWhile(l => !l.Contains("Recommended Actions")).Skip(1).TakeWhile(l => !l.StartsWith("**")).ToList();
        
        if (recSection.Any())
        {
            return string.Join(" ", recSection).Trim();
        }

        // Fallback: return first paragraph
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
        // Simple confidence based on sample size and concentration
        var sampleScore = Math.Min(categoryCount / 10.0, 1.0);
        var concentrationScore = categoryCount / (double)totalRequests;
        return (sampleScore + concentrationScore) / 2.0;
    }
}