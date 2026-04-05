using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;

namespace CampusConnect.Pages.Manager.AIInsights;

[Authorize(Roles = "Manager,Admin")]
public class IndexModel : PageModel
{
    private readonly MongoDBService _mongoService;

    public IndexModel(MongoDBService mongoService)
    {
        _mongoService = mongoService;
    }

    public List<AIInsight> ActiveInsights { get; set; } = new();
    public List<AIInsight> ResolvedInsights { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? FilterSeverity { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterBuilding { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var filter = Builders<AIInsight>.Filter.Empty;

        // Apply filters
        if (!string.IsNullOrEmpty(FilterSeverity))
        {
            filter &= Builders<AIInsight>.Filter.Eq(i => i.Severity, FilterSeverity);
        }

        if (!string.IsNullOrEmpty(FilterBuilding))
        {
            filter &= Builders<AIInsight>.Filter.Eq(i => i.BuildingName, FilterBuilding);
        }

        // Get all insights
        var allInsights = await _mongoService.AIInsights
            .Find(filter)
            .SortByDescending(i => i.GeneratedAt)
            .ToListAsync();

        ActiveInsights = allInsights.Where(i => i.ResolvedAt == null).ToList();
        ResolvedInsights = allInsights.Where(i => i.ResolvedAt != null).ToList();

        return Page();
    }
}