using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public string? RequestId { get; private set; }
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);

        // Used for status-code pages (/Error/404 etc.)
        public int? StatusCode { get; private set; }

        public void OnGet(int? statusCode = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            StatusCode = statusCode;

            // 1) Unhandled exceptions routed by UseExceptionHandler("/Error")
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                _logger.LogError(
                    exceptionFeature.Error,
                    "UNHANDLED EXCEPTION. Path={Path} StatusCode={StatusCode} TraceId={TraceId}",
                    exceptionFeature.Path,
                    StatusCode,
                    RequestId
                );

                return;
            }

            // 2) Non-success status codes routed by UseStatusCodePagesWithReExecute("/Error/{0}")
            if (StatusCode.HasValue)
            {
                var originalPath = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;
                var originalQueryString = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalQueryString;

                // Log 404/403/etc as warnings (not errors)
                _logger.LogWarning(
                    "HTTP STATUS CODE. StatusCode={StatusCode} OriginalPath={OriginalPath} OriginalQueryString={OriginalQueryString} TraceId={TraceId}",
                    StatusCode,
                    originalPath,
                    originalQueryString,
                    RequestId
                );
            }
        }
    }
}