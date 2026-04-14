using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;

namespace CampusConnect.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TestApiKeyModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TestApiKeyModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public TestApiKeyModel(
            IConfiguration config, 
            ILogger<TestApiKeyModel> logger,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public string ApiKey { get; set; } = string.Empty;
        public string ApiKeyPreview { get; set; } = string.Empty;
        public string TestResult { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task OnGetAsync()
        {
            ApiKey = _config["Gemini:ApiKey"] ?? "NOT FOUND IN CONFIG";
            ApiKeyPreview = ApiKey.Length > 10 ? ApiKey.Substring(0, 10) + "..." : ApiKey;

            if (ApiKey == "NOT FOUND IN CONFIG")
            {
                TestResult = "❌ API Key not found in appsettings.json\n\nCheck that Gemini:ApiKey is set correctly.";
                IsSuccess = false;
                return;
            }

            // Test the API directly
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={ApiKey}";

            try
            {
                _logger.LogInformation("Testing Gemini API with key: {Preview}", ApiKeyPreview);

                var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TestResult = $"✅ SUCCESS!\n\nStatus: {response.StatusCode}\n\nResponse:\n{body}";
                    IsSuccess = true;
                }
                else
                {
                    TestResult = $"❌ FAILED\n\nStatus: {response.StatusCode}\n\nResponse:\n{body}";
                    IsSuccess = false;
                }

                _logger.LogInformation("API Response: {Status} - {Body}", response.StatusCode, body);
            }
            catch (Exception ex)
            {
                TestResult = $"❌ EXCEPTION\n\nError: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                IsSuccess = false;
                _logger.LogError(ex, "API test failed");
            }
        }
    }
}