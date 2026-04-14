using CampusConnect.Configuration;
using CampusConnect.Models.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

namespace CampusConnect.Services;

public class MongoDBService
{
    private readonly IMongoDatabase? _database;
    private readonly ILogger<MongoDBService> _logger;
    private bool _isAvailable = false;

    public MongoDBService(IOptions<MongoDBSettings> settings, ILogger<MongoDBService> logger)
    {
        _logger = logger;
        
        try
        {
            var mongoSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
            
            // Reduce timeout to fail faster
            mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(5);
            mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            
            // Enable retries
            mongoSettings.RetryWrites = true;
            mongoSettings.RetryReads = true;
            
            var client = new MongoClient(mongoSettings);
            _database = client.GetDatabase(settings.Value.DatabaseName);
            _isAvailable = true;
            
            _logger.LogInformation("MongoDB connection initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB connection. Service will operate in degraded mode.");
            _isAvailable = false;
        }
    }

    // Existing collections
    public IMongoCollection<RequestSnapshot>? RequestSnapshots =>
        _database?.GetCollection<RequestSnapshot>("requestSnapshots");

    public IMongoCollection<AIInsight>? AIInsights =>
        _database?.GetCollection<AIInsight>("aiInsights");

    public IMongoCollection<ActivityLog>? ActivityLogs =>
        _database?.GetCollection<ActivityLog>("activityLogs");

    // NEW: Activity Log Methods with error handling
    public async Task LogActivityAsync(ActivityLog log)
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            _logger.LogWarning("MongoDB unavailable. Activity log not saved: {Action} by {User}", 
                log.Action, log.UserName);
            return; // Silently fail - don't block the application
        }

        try
        {
            log.Timestamp = DateTime.UtcNow;
            await ActivityLogs.InsertOneAsync(log);
            _logger.LogDebug("Activity logged: {Action} by {User}", log.Action, log.UserName);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to log activity to MongoDB: {Action} by {User}", 
                log.Action, log.UserName);
            // Don't throw - allow the application to continue
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "MongoDB timeout while logging activity: {Action} by {User}", 
                log.Action, log.UserName);
            // Don't throw - allow the application to continue
        }
    }

    public async Task<List<ActivityLog>> GetRecentActivityAsync(int limit = 50)
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            _logger.LogWarning("MongoDB unavailable. Returning empty activity list.");
            return new List<ActivityLog>();
        }

        try
        {
            return await ActivityLogs
                .Find(_ => true)
                .SortByDescending(a => a.Timestamp)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve activity logs from MongoDB");
            return new List<ActivityLog>();
        }
    }

    public async Task<List<ActivityLog>> GetActivityByUserAsync(string userId)
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            return new List<ActivityLog>();
        }

        try
        {
            return await ActivityLogs
                .Find(a => a.UserId == userId)
                .SortByDescending(a => a.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user activity from MongoDB");
            return new List<ActivityLog>();
        }
    }

    public async Task<List<ActivityLog>> GetActivityByRequestAsync(int requestId)
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            return new List<ActivityLog>();
        }

        try
        {
            return await ActivityLogs
                .Find(a => a.RequestId == requestId)
                .SortByDescending(a => a.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve request activity from MongoDB");
            return new List<ActivityLog>();
        }
    }

    public async Task<ActivityLog?> GetActivityByIdAsync(string id)
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            return null;
        }

        try
        {
            return await ActivityLogs
                .Find(a => a.Id == id)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve activity by ID from MongoDB");
            return null;
        }
    }

    public async Task<bool> UpdateActivityLogAsync(string id, string reviewedBy, string reviewNotes)
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            return false;
        }

        try
        {
            var filter = Builders<ActivityLog>.Filter.Eq(a => a.Id, id);
            var update = Builders<ActivityLog>.Update
                .Set(a => a.Reviewed, true)
                .Set(a => a.ReviewedBy, reviewedBy)
                .Set(a => a.ReviewNotes, reviewNotes);

            var result = await ActivityLogs.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update activity log in MongoDB");
            return false;
        }
    }

    public async Task<bool> DeleteActivityLogAsync(string id)
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            return false;
        }

        try
        {
            var result = await ActivityLogs.DeleteOneAsync(a => a.Id == id);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete activity log from MongoDB");
            return false;
        }
    }

    public async Task<long> GetActivityLogCountAsync()
    {
        if (!_isAvailable || _database == null || ActivityLogs == null)
        {
            return 0;
        }

        try
        {
            return await ActivityLogs.CountDocumentsAsync(_ => true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count activity logs in MongoDB");
            return 0;
        }
    }

    // Add similar error handling to your other methods...
}