using CampusConnect.Configuration;
using CampusConnect.Models.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CampusConnect.Services;

public class MongoDBService
{
    private readonly IMongoDatabase _database;

    public MongoDBService(IOptions<MongoDBSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    // Existing collections
    public IMongoCollection<RequestSnapshot> RequestSnapshots =>
        _database.GetCollection<RequestSnapshot>("requestSnapshots");

    public IMongoCollection<AIInsight> AIInsights =>
        _database.GetCollection<AIInsight>("aiInsights");

    // NEW: ActivityLogs collection
    public IMongoCollection<ActivityLog> ActivityLogs =>
        _database.GetCollection<ActivityLog>("activityLogs");

    // NEW: Activity Log Methods
    public async Task LogActivityAsync(ActivityLog log)
    {
        log.Timestamp = DateTime.UtcNow;
        await ActivityLogs.InsertOneAsync(log);
    }

    public async Task<List<ActivityLog>> GetRecentActivityAsync(int limit = 50)
    {
        return await ActivityLogs
            .Find(_ => true)
            .SortByDescending(a => a.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetActivityByUserAsync(string userId)
    {
        return await ActivityLogs
            .Find(a => a.UserId == userId)
            .SortByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetActivityByRequestAsync(int requestId)
    {
        return await ActivityLogs
            .Find(a => a.RequestId == requestId)
            .SortByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<ActivityLog?> GetActivityByIdAsync(string id)
    {
        return await ActivityLogs
            .Find(a => a.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateActivityLogAsync(string id, string reviewedBy, string reviewNotes)
    {
        var filter = Builders<ActivityLog>.Filter.Eq(a => a.Id, id);
        var update = Builders<ActivityLog>.Update
            .Set(a => a.Reviewed, true)
            .Set(a => a.ReviewedBy, reviewedBy)
            .Set(a => a.ReviewNotes, reviewNotes);

        var result = await ActivityLogs.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteActivityLogAsync(string id)
    {
        var result = await ActivityLogs.DeleteOneAsync(a => a.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<long> GetActivityLogCountAsync()
    {
        return await ActivityLogs.CountDocumentsAsync(_ => true);
    }

    // Existing methods for RequestSnapshots and AIInsights...
    // (keep all your existing code)
}