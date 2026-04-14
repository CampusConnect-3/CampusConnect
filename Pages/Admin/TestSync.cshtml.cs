using CampusConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class TestSyncModel : PageModel
{
    private readonly RequestSyncService _syncService;

    public TestSyncModel(RequestSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<IActionResult> OnPostAsync(int requestId)
    {
        var result = await _syncService.SyncRequestToMongoDBAsync(requestId);
        return new JsonResult(new { success = result });
    }
}