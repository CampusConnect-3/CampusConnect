using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampusConnect.Models.MongoDB;

public class RequestSnapshot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("requestId")]
    public int RequestId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("priority")]
    public string Priority { get; set; } = string.Empty;

    [BsonElement("category")]
    public CategoryInfo Category { get; set; } = new();

    [BsonElement("building")]
    public BuildingInfo Building { get; set; } = new();

    [BsonElement("createdBy")]
    public UserInfo CreatedBy { get; set; } = new();

    [BsonElement("assignedTo")]
    public UserInfo? AssignedTo { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("assignedAt")]
    public DateTime? AssignedAt { get; set; }

    [BsonElement("syncedAt")]
    public DateTime SyncedAt { get; set; }

    [BsonElement("timeToAssignment")]
    public TimeSpan? TimeToAssignment { get; set; }

    [BsonElement("timeToCompletion")]
    public TimeSpan? TimeToCompletion { get; set; }

    [BsonElement("extractedKeywords")]
    public List<string> ExtractedKeywords { get; set; } = new();
}

public class CategoryInfo
{
    [BsonElement("categoryId")]
    public int CategoryId { get; set; }

    [BsonElement("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [BsonElement("parentCategory")]
    public string? ParentCategory { get; set; }
}

public class BuildingInfo
{
    [BsonElement("buildingId")]
    public int BuildingId { get; set; }

    [BsonElement("buildingName")]
    public string BuildingName { get; set; } = string.Empty;

    [BsonElement("buildingCode")]
    public string? BuildingCode { get; set; }

    [BsonElement("campus")]
    public string? Campus { get; set; }

    [BsonElement("zone")]
    public string? Zone { get; set; }
}

public class UserInfo
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("role")]
    public string? Role { get; set; }

    [BsonElement("name")]
    public string? Name { get; set; }
}