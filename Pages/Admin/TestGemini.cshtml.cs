using CampusConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace CampusConnect.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TestGeminiModel : PageModel
    {
        private readonly GeminiInsightService _geminiService;

        public TestGeminiModel(GeminiInsightService geminiService)
        {
            _geminiService = geminiService;
        }

        public List<string> AvailableModels { get; set; } = new();

        public async Task OnGetAsync()
        {
            AvailableModels = await _geminiService.ListAvailableModelsAsync();
        }
    }
}