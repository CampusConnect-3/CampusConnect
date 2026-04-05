using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Models.MongoDB;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services;

public class RequestSyncService
{
    private readonly TablesDbContext _dbContext;
    private readonly MongoDBService _mongoService;
    private readonly ILogger<RequestSyncService> _logger;

    public RequestSyncService(
        TablesDbContext dbContext,
        MongoDBService mongoService,
        ILogger<RequestSyncService> logger)
    {
        _dbContext = dbContext;
        _mongoService = mongoService;
        _logger = logger;
    }

    /// <summary>
    /// Syncs a single request to MongoDB (for real-time sync on create/update)
    /// </summary>
    public async Task SyncRequestAsync(int requestId)
    {
        try
        {
            var request = await _dbContext.request
                .Include(r => r.createdBy)
                    .ThenInclude(u => u.userRoles)
                    .ThenInclude(ur => ur.role)
                .Include(r => r.assignedTo)
                    .ThenInclude(u => u.userRoles)
                    .ThenInclude(ur => ur.role)
                .Include(r => r.category)
                .Include(r => r.status)
                .FirstOrDefaultAsync(r => r.requestID == requestId);

            if (request == null)
            {
                _logger.LogWarning("Request {RequestId} not found for sync", requestId);
                return;
            }

            var snapshot = MapToSnapshot(request);
            
            // Upsert (update if exists, insert if new)
            var filter = MongoDB.Driver.Builders<RequestSnapshot>.Filter.Eq(r => r.RequestId, requestId);
            await _mongoService.RequestSnapshots.ReplaceOneAsync(
                filter, 
                snapshot, 
                new MongoDB.Driver.ReplaceOptions { IsUpsert = true });

            _logger.LogInformation("Successfully synced request {RequestId} to MongoDB", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing request {RequestId} to MongoDB", requestId);
            throw;
        }
    }

    /// <summary>
    /// Syncs all requests (for initial load or batch operations)
    /// </summary>
    public async Task SyncAllRequestsAsync()
    {
        try
        {
            var requests = await _dbContext.request
                .Include(r => r.createdBy)
                    .ThenInclude(u => u.userRoles)
                    .ThenInclude(ur => ur.role)
                .Include(r => r.assignedTo)
                    .ThenInclude(u => u.userRoles)
                    .ThenInclude(ur => ur.role)
                .Include(r => r.category)
                .Include(r => r.status)
                .ToListAsync();

            var snapshots = requests.Select(MapToSnapshot).ToList();

            if (snapshots.Any())
            {
                // Clear existing and insert fresh data
                await _mongoService.RequestSnapshots.DeleteManyAsync(MongoDB.Driver.Builders<RequestSnapshot>.Filter.Empty);
                await _mongoService.RequestSnapshots.InsertManyAsync(snapshots);
            }

            _logger.LogInformation("Successfully synced {Count} requests to MongoDB", snapshots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all requests to MongoDB");
            throw;
        }
    }

    /// <summary>
    /// Maps SQL request entity to MongoDB snapshot document
    /// </summary>
    private RequestSnapshot MapToSnapshot(request request)
    {
        var snapshot = new RequestSnapshot
        {
            RequestId = request.requestID,
            Title = request.title,
            Description = request.description,
            Priority = request.priority,
            Status = request.status?.statusName ?? "Unknown",
            CreatedAt = request.createdAt,
            CompletedAt = request.closedAt,
            SyncedAt = DateTime.UtcNow,
            
            Building = new BuildingInfo
            {
                BuildingId = GetBuildingId(request.buildingName), // Hash building name to get consistent ID
                BuildingName = request.buildingName,
                BuildingCode = ExtractBuildingCode(request.buildingName),
                Campus = "Main Campus", // You can make this dynamic if you have campus data
                Zone = DetermineZone(request.buildingName)
            },
            
            Category = new CategoryInfo
            {
                CategoryId = request.categoryID,
                CategoryName = request.category?.categoryName ?? "Unknown",
                ParentCategory = DetermineParentCategory(request.category?.categoryName)
            },
            
            CreatedBy = new UserInfo
            {
                UserId = request.created_by.ToString(),
                FullName = request.createdBy != null 
                    ? $"{request.createdBy.fName} {request.createdBy.lName}".Trim() 
                    : "Unknown",
                Email = request.email,
                Role = GetUserRole(request.createdBy)
            },
            
            AssignedTo = request.assignedTo != null 
                ? new UserInfo
                {
                    UserId = request.assigned_to.ToString()!,
                    FullName = $"{request.assignedTo.fName} {request.assignedTo.lName}".Trim(),
                    Email = request.assignedTo.email,
                    Role = GetUserRole(request.assignedTo)
                }
                : null,
            
            ExtractedKeywords = ExtractKeywords(request.title, request.description)
        };

        // Calculate metrics
        if (request.assigned_to.HasValue && request.createdAt != default)
        {
            // You may need to track when assignment happened - for now estimate
            snapshot.AssignedAt = request.createdAt.AddHours(2); // Placeholder
            snapshot.TimeToAssignment = snapshot.AssignedAt - request.createdAt;
        }

        if (request.closedAt.HasValue)
        {
            snapshot.TimeToCompletion = request.closedAt - request.createdAt;
        }

        return snapshot;
    }

    /// <summary>
    /// Gets a consistent building ID from building name (using hash)
    /// </summary>
    private int GetBuildingId(string buildingName)
    {
        // Simple hash to generate consistent ID from building name
        return Math.Abs(buildingName.GetHashCode() % 10000);
    }

    /// <summary>
    /// Extracts building code from building name (first letters or abbreviation)
    /// </summary>
    private string? ExtractBuildingCode(string buildingName)
    {
        var words = buildingName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
            return buildingName.Substring(0, Math.Min(3, buildingName.Length)).ToUpper();
        
        return string.Join("", words.Select(w => w[0])).ToUpper();
    }

    /// <summary>
    /// Determines zone based on building name (you can customize this)
    /// </summary>
    private string? DetermineZone(string buildingName)
    {
        // Example logic - customize based on your campus
        if (buildingName.Contains("Hall", StringComparison.OrdinalIgnoreCase))
            return "Residential";
        if (buildingName.Contains("Library", StringComparison.OrdinalIgnoreCase) || 
            buildingName.Contains("Center", StringComparison.OrdinalIgnoreCase))
            return "Academic";
        if (buildingName.Contains("Gym", StringComparison.OrdinalIgnoreCase) || 
            buildingName.Contains("Stadium", StringComparison.OrdinalIgnoreCase))
            return "Athletic";
        
        return "Academic"; // Default
    }

    /// <summary>
    /// Determines parent category for better grouping
    /// </summary>
    private string? DetermineParentCategory(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return null;

        var facilities = new[] { "HVAC", "Plumbing", "Electrical", "Structural" };
        var technology = new[] { "IT", "Network", "Computer", "WiFi" };
        var safety = new[] { "Security", "Fire", "Emergency" };
        var grounds = new[] { "Landscaping", "Parking", "Exterior" };

        if (facilities.Any(f => categoryName.Contains(f, StringComparison.OrdinalIgnoreCase)))
            return "Facilities";
        if (technology.Any(t => categoryName.Contains(t, StringComparison.OrdinalIgnoreCase)))
            return "Technology";
        if (safety.Any(s => categoryName.Contains(s, StringComparison.OrdinalIgnoreCase)))
            return "Safety & Security";
        if (grounds.Any(g => categoryName.Contains(g, StringComparison.OrdinalIgnoreCase)))
            return "Grounds & Exterior";

        return "General Maintenance";
    }

    /// <summary>
    /// Gets the primary role for a user
    /// </summary>
    private string? GetUserRole(user? user)
    {
        if (user?.userRoles == null || !user.userRoles.Any())
            return null;

        var primaryRole = user.userRoles.FirstOrDefault()?.role?.roleName;
        return primaryRole;
    }

    /// <summary>
    /// Extracts keywords from text for pattern matching
    /// </summary>
    private List<string> ExtractKeywords(string title, string description)
    {
        var text = $"{title} {description}".ToLower();
        var keywords = new List<string>();

        // Common issue keywords - EXPANDED
        var patterns = new Dictionary<string, string[]>
        {
            // Water/Plumbing
            ["water"] = new[] { "leak", "leaking", "water", "flood", "flooding", "drip", "dripping", "overflow", "pipe", "faucet", "sink", "drain", "clog", "clogged", "toilet", "shower", "bath" },
            
            // HVAC/Temperature
            ["hvac"] = new[] { "cold", "hot", "heat", "heating", "ac", "a/c", "air conditioning", "hvac", "temperature", "thermostat", "freezing", "warm", "cool", "cooling", "vent", "ventilation" },
            
            // Electrical
            ["electrical"] = new[] { "light", "lights", "bulb", "dark", "electrical", "electric", "power", "outlet", "switch", "breaker", "flickering", "spark" },
            
            // Structural
            ["structural"] = new[] { "broken", "damage", "damaged", "crack", "cracked", "hole", "wall", "ceiling", "floor", "door", "lock", "window", "broken glass" },
            
            // Noise
            ["noise"] = new[] { "noise", "noisy", "loud", "sound", "bang", "rattle", "buzz", "humming" },
            
            // Smell
            ["smell"] = new[] { "smell", "odor", "stink", "foul", "musty", "mold", "mildew" },
            
            // Pests
            ["pest"] = new[] { "pest", "pests", "bug", "bugs", "rodent", "rodents", "mouse", "mice", "rat", "rats", "insect", "cockroach", "ant", "ants" },
            
            // Technology
            ["technology"] = new[] { "wifi", "wi-fi", "internet", "network", "computer", "printer", "projector", "screen" },
            
            // Safety
            ["safety"] = new[] { "fire", "smoke", "alarm", "emergency", "safety", "hazard", "danger", "unsafe" }
        };

        foreach (var category in patterns)
        {
            foreach (var pattern in category.Value)
            {
                if (text.Contains(pattern))
                {
                    keywords.Add(pattern);
                }
            }
        }

        return keywords.Distinct().OrderBy(k => k).ToList();
    }
}