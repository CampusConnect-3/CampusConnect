using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampusConnect.Models.MongoDB;

public class AIInsight
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string InsightType { get; set; } = null!;
    public string Severity { get; set; } = null!;
    
    public int? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? CategoryName { get; set; }
    
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Recommendation { get; set; } = null!;
    
    public InsightMetrics Metrics { get; set; } = null!;
    
    public List<int> RelatedRequestIds { get; set; } = new();
    
    public DateTime GeneratedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    
    public string? AIModel { get; set; }
    public double? ConfidenceScore { get; set; }
}

public class InsightMetrics
{
    public int RequestCount { get; set; }
    public int DaySpan { get; set; }
    public double IncreasePercentage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}