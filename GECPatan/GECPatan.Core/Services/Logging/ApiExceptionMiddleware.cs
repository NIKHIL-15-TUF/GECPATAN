using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GECPatan.Core.Services.Logging
{
    /// <summary>
    /// For the Api (which has no exception handler today): logs any unhandled exception with its
    /// request context and returns the same JSON envelope the controllers use
    /// ({ success, message, data, total }) instead of an empty 500.
    /// The client only gets a reference id; the stack trace stays in ApplicationLogs.
    /// </summary>
    public class ApiExceptionMiddleware
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;
        private readonly bool _includeDetails;

        /// <param name="includeDetails">Pass env.IsDevelopment() - adds the exception message to the response.</param>
        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, bool includeDetails = false)
        {
            _next = next; _logger = logger; _includeDetails = includeDetails;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
            {
                // Client closed the connection - not an error.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in {Method} {Path}", ctx.Request.Method, ctx.Request.Path.Value);

                if (ctx.Response.HasStarted) return;   // too late to change the response

                ctx.Response.Clear();
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                ctx.Response.ContentType = "application/json";

                var message = $"An unexpected error occurred. Reference: {ctx.TraceIdentifier}";
                if (_includeDetails) message += $" ({ex.GetType().Name}: {ex.Message})";

                await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                    new { success = false, message, data = (object?)null, total = (int?)null }, Json));
            }
        }
    }
}
