using CampusConnect.Data;
using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace CampusConnect.Pages.Manager.AIInsights;

[Authorize(Roles = "Manager,Admin")]
public class DetailsModel : PageModel
{
    private readonly MongoDBService _mongoService;
    private readonly TablesDbContext _dbContext;

    public DetailsModel(MongoDBService mongoService, TablesDbContext dbContext)
    {
        _mongoService = mongoService;
        _dbContext = dbContext;
    }

    public AIInsight Insight { get; set; } = null!;
    public List<Models.request> RelatedRequests { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Insight = await _mongoService.AIInsights
            .Find(i => i.Id == id)
            .FirstOrDefaultAsync();

        if (Insight == null)
        {
            return NotFound();
        }

        // Load related requests
        if (Insight.RelatedRequestIds.Any())
        {
            RelatedRequests = await _dbContext.request
                .Include(r => r.createdBy)
                .Include(r => r.category)
                .Include(r => r.status)
                .Where(r => Insight.RelatedRequestIds.Contains(r.requestID))
                .ToListAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(string id)
    {
        var userName = User.Identity?.Name ?? "Unknown";
        
        var update = Builders<AIInsight>.Update
            .Set(i => i.AcknowledgedAt, DateTime.UtcNow)
            .Set(i => i.AcknowledgedBy, userName);

        await _mongoService.AIInsights.UpdateOneAsync(
            i => i.Id == id,
            update);

        return RedirectToPage("./Details", new { id });
    }

    public async Task<IActionResult> OnPostResolveAsync(string id, string resolutionNotes)
    {
        var userName = User.Identity?.Name ?? "Unknown";
        
        var update = Builders<AIInsight>.Update
            .Set(i => i.ResolvedAt, DateTime.UtcNow)
            .Set(i => i.ResolutionNotes, resolutionNotes)
            .Set(i => i.AcknowledgedBy, userName);

        await _mongoService.AIInsights.UpdateOneAsync(
            i => i.Id == id,
            update);

        return RedirectToPage("./Index");
    }
}