using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampusConnect.Models.MongoDB
{
    public class ActivityLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("userName")]
        public string UserName { get; set; } = string.Empty;

        [BsonElement("userRole")]
        public string UserRole { get; set; } = string.Empty;

        [BsonElement("action")]
        public string Action { get; set; } = string.Empty; // "created_request", "status_changed", "assigned_request"

        [BsonElement("requestId")]
        public int? RequestId { get; set; }

        [BsonElement("requestTitle")]
        public string? RequestTitle { get; set; }

        [BsonElement("details")]
        public BsonDocument Details { get; set; } = new BsonDocument();

        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }

        [BsonElement("reviewed")]
        public bool Reviewed { get; set; } = false;

        [BsonElement("reviewedBy")]
        public string? ReviewedBy { get; set; }

        [BsonElement("reviewNotes")]
        public string? ReviewNotes { get; set; }
    }
}