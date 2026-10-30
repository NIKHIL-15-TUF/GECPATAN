using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Http;

namespace GECPatan.Core.Services.Security
{
    /// <summary>
    /// Watches the response status leaving the pipeline and records access-control failures:
    /// 401 (not authenticated), 403 (forbidden) and 429 (rate limit hit).
    /// Register it EARLY (right after UseHttpsRedirection) so it also sees responses produced
    /// by later middleware such as the rate limiter.
    /// </summary>
    public class SecurityEventMiddleware
    {
        private readonly RequestDelegate _next;
        public SecurityEventMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, ISecurityLogService security)
        {
            await _next(ctx);

            var status = ctx.Response.StatusCode;
            string? type = status switch
            {
                StatusCodes.Status401Unauthorized => SecurityEvents.UnauthorizedAccess,
                StatusCodes.Status403Forbidden => SecurityEvents.AccessDenied,
                StatusCodes.Status429TooManyRequests => SecurityEvents.RateLimitExceeded,
                _ => null
            };
            if (type == null) return;

            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = ctx.Request.Path.Value ?? "";

            await security.LogAsync(
                ctx, type, SecuritySeverity.Warning,
                details: $"{ctx.Request.Method} {path} returned {status}",
                statusCode: status,
                throttleKey: $"{type}|{ip}|{path}");
        }
    }
}
