using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampusConnect.Models.MongoDB;

public class BuildingAnalytics
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = null!;
    
    public int TotalRequests { get; set; }
    public int OpenRequests { get; set; }
    public int ClosedRequests { get; set; }
    
    public List<CategoryBreakdown> CategoryBreakdowns { get; set; } = new();
    public Dictionary<string, int> PriorityDistribution { get; set; } = new();
    
    public double AverageTimeToAssignmentHours { get; set; }
    public double AverageTimeToCompletionHours { get; set; }
    
    public double RequestTrendPercentage { get; set; }
    
    public DateTime LastUpdated { get; set; }
}

public class CategoryBreakdown
{
    public string CategoryName { get; set; } = null!;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public List<string> CommonKeywords { get; set; } = new();
    public double TrendPercentage { get; set; }
}