using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampusConnect.Models.MongoDB;

public class IssuePattern
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string PatternName { get; set; } = null!;
    public string PatternType { get; set; } = null!;
    
    public int? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string CategoryName { get; set; } = null!;
    
    public List<string> CommonSymptoms { get; set; } = new();
    public List<int> AffectedRequestIds { get; set; } = new();
    public int Frequency { get; set; }
    
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public string? TemporalPattern { get; set; }
    
    public bool IsActive { get; set; }
    public bool HasGeneratedInsight { get; set; }
    public string? RelatedInsightId { get; set; }
    
    public DateTime DetectedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}