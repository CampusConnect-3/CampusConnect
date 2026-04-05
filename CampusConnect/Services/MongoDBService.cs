using CampusConnect.Configuration;
using CampusConnect.Models.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CampusConnect.Services;

public class MongoDBService
{
    private readonly IMongoDatabase _database;
    
    public IMongoCollection<RequestSnapshot> RequestSnapshots { get; }
    public IMongoCollection<BuildingAnalytics> BuildingAnalytics { get; }
    public IMongoCollection<AIInsight> AIInsights { get; }
    public IMongoCollection<IssuePattern> IssuePatterns { get; }

    public MongoDBService(IOptions<MongoDBSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        
        // Initialize collections
        RequestSnapshots = _database.GetCollection<RequestSnapshot>("request_snapshots");
        BuildingAnalytics = _database.GetCollection<BuildingAnalytics>("building_analytics");
        AIInsights = _database.GetCollection<AIInsight>("ai_insights");
        IssuePatterns = _database.GetCollection<IssuePattern>("issue_patterns");
        
        // Create indexes for performance
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        // Request Snapshots indexes
        var requestIndexKeys = Builders<RequestSnapshot>.IndexKeys
            .Ascending(r => r.RequestId)
            .Ascending(r => r.Building.BuildingId)
            .Ascending(r => r.Category.CategoryName)
            .Descending(r => r.CreatedAt);
        RequestSnapshots.Indexes.CreateOne(new CreateIndexModel<RequestSnapshot>(requestIndexKeys));
        
        // Building Analytics indexes
        var buildingIndexKeys = Builders<BuildingAnalytics>.IndexKeys
            .Ascending(b => b.BuildingId)
            .Descending(b => b.PeriodStart);
        BuildingAnalytics.Indexes.CreateOne(new CreateIndexModel<BuildingAnalytics>(buildingIndexKeys));
        
        // AI Insights indexes
        var insightIndexKeys = Builders<AIInsight>.IndexKeys
            .Ascending(i => i.BuildingId)
            .Ascending(i => i.Severity)
            .Descending(i => i.GeneratedAt);
        AIInsights.Indexes.CreateOne(new CreateIndexModel<AIInsight>(insightIndexKeys));
    }
}