using System.Diagnostics;

namespace Viper.Classes
{
    /// <summary>
    /// Middleware that ensures every request has a correlation ID for tracking across systems.
    /// Uses Activity.TraceId when available (standard .NET distributed tracing) or generates a new GUID.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Use existing Activity.TraceId (from .NET distributed tracing) or generate new one
            var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

            // Store in HttpContext for access throughout the request pipeline
            context.Items["CorrelationId"] = correlationId;

            // Add to response headers for client-side debugging and logging
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            // Set up NLog context so correlation ID is automatically included in all log entries
            using (NLog.ScopeContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogDebug("Request started with correlation ID: {CorrelationId}", correlationId);
                await _next(context);
                _logger.LogDebug("Request completed with correlation ID: {CorrelationId}", correlationId);
            }
        }
    }

    /// <summary>
    /// Extension method for registering CorrelationIdMiddleware in the pipeline
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
