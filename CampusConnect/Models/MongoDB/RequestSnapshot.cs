using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampusConnect.Models.MongoDB;

public class RequestSnapshot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public int RequestId { get; set; }
    
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    
    public BuildingInfo Building { get; set; } = null!;
    public CategoryInfo Category { get; set; } = null!;
    
    public string Priority { get; set; } = null!;
    public string Status { get; set; } = null!;
    
    public UserInfo CreatedBy { get; set; } = null!;
    public UserInfo? AssignedTo { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime SyncedAt { get; set; }
    
    public TimeSpan? TimeToAssignment { get; set; }
    public TimeSpan? TimeToCompletion { get; set; }
    
    public List<string> ExtractedKeywords { get; set; } = new();
}

public class BuildingInfo
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public string? BuildingCode { get; set; }
    public string? Campus { get; set; }
    public string? Zone { get; set; }
}

public class CategoryInfo
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? ParentCategory { get; set; }
}

public class UserInfo
{
    public string UserId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Role { get; set; }
}