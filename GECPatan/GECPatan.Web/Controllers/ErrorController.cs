using System.Diagnostics;
using GECPatan.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    /// <summary>
    /// Wiring (add to Program.cs — see chat for the full snippet):
    ///   app.UseExceptionHandler("/Error");                 // unhandled exceptions -> Index()
    ///   app.UseStatusCodePagesWithReExecute("/Error/{0}"); // 404/403/etc -> HttpStatusCodeHandler(statusCode)
    /// </summary>
    [Route("Error")]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        // Reached via UseExceptionHandler for any unhandled exception.
        [Route("")]
        [Route("/Error")]
        public IActionResult Index()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (exceptionFeature != null)
            {
                // Full exception detail goes to the log only — the visitor
                // only ever sees the request id, never the message or stack
                // trace.
                _logger.LogError(exceptionFeature.Error,
                    "Unhandled exception on {Path} (request {RequestId})",
                    exceptionFeature.Path, requestId);
            }

            var vm = new ErrorViewModel
            {
                StatusCode = 500,
                Title = "Something went wrong",
                Message = "An unexpected error occurred on our end. Our team has been notified — please try again in a few minutes, or contact us if the problem continues.",
                ShowGoHome = true,
                ShowContactUs = true,
                RequestId = requestId
            };

            Response.StatusCode = 500;
            return View("Index", vm);
        }

        // Reached via UseStatusCodePagesWithReExecute for non-exception error
        // responses (404, 403, 400, ...).
        [Route("{statusCode:int}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var requestId = HttpContext.TraceIdentifier;
            var originalPath = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;

            _logger.LogWarning(
                "Status code {StatusCode} for {Path} (request {RequestId})",
                statusCode, originalPath, requestId);

            var vm = statusCode switch
            {
                404 => new ErrorViewModel
                {
                    StatusCode = 404,
                    Title = "Page not found",
                    Message = "The page you're looking for doesn't exist or may have been moved.",
                    ShowGoHome = true,
                    ShowContactUs = true
                },
                403 => new ErrorViewModel
                {
                    StatusCode = 403,
                    Title = "Access denied",
                    Message = "You don't have permission to view this page.",
                    ShowGoHome = true,
                    ShowContactUs = true
                },
                400 => new ErrorViewModel
                {
                    StatusCode = 400,
                    Title = "Bad request",
                    Message = "We couldn't process that request. Please check the link and try again.",
                    ShowGoHome = true,
                    ShowContactUs = false
                },
                _ => new ErrorViewModel
                {
                    StatusCode = statusCode,
                    Title = "Something went wrong",
                    Message = "An unexpected error occurred while handling your request.",
                    ShowGoHome = true,
                    ShowContactUs = true
                }
            };

            vm.RequestId = requestId;
            Response.StatusCode = statusCode;
            return View("Index", vm);
        }
    }
}