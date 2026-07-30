using System.Diagnostics;
using GECPatan.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Admin.Controllers
{
    /// <summary>
    /// Wiring (add to Program.cs — see chat for the full snippet):
    ///   app.UseExceptionHandler("/Error");
    ///   app.UseStatusCodePagesWithReExecute("/Error/{0}");
    ///
    /// 403 is NOT handled here — it's already covered by the existing
    /// Access Denied page via CookieAuthenticationOptions.AccessDeniedPath.
    /// If that redirect ever stops firing before this pipeline runs,
    /// HttpStatusCodeHandler below still has a 403 branch as a fallback.
    /// </summary>
    [AllowAnonymous] // errors can happen before/without login (broken links, expired sessions)
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
                // Full exception detail goes to the log only — never shown
                // to the admin, just the request id for support correlation.
                _logger.LogError(exceptionFeature.Error,
                    "Unhandled exception on {Path} (request {RequestId})",
                    exceptionFeature.Path, requestId);
            }

            var vm = new ErrorViewModel
            {
                StatusCode = 500,
                Title = "Something went wrong",
                Message = "An unexpected error occurred. Please try again, or contact the system administrator if it continues.",
                ShowGoToDashboard = true,
                RequestId = requestId
            };

            Response.StatusCode = 500;
            return View("Index", vm);
        }

        // Reached via UseStatusCodePagesWithReExecute for non-exception error
        // responses (404, 400, etc.). 403 included only as a fallback.
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
                    ShowGoToDashboard = true
                },
                403 => new ErrorViewModel
                {
                    StatusCode = 403,
                    Title = "Access denied",
                    Message = "You don't have permission to view this page.",
                    ShowGoToDashboard = true
                },
                400 => new ErrorViewModel
                {
                    StatusCode = 400,
                    Title = "Bad request",
                    Message = "We couldn't process that request. Please check the link and try again.",
                    ShowGoToDashboard = true
                },
                _ => new ErrorViewModel
                {
                    StatusCode = statusCode,
                    Title = "Something went wrong",
                    Message = "An unexpected error occurred while handling your request.",
                    ShowGoToDashboard = true
                }
            };

            vm.RequestId = requestId;
            Response.StatusCode = statusCode;
            return View("Index", vm);
        }
    }
}