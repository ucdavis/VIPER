using Viper.Classes;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// Base controller for Effort area providing shared functionality.
/// Inherits from ApiController to get standardized exception handling, response formatting, and filters.
/// </summary>
public abstract class BaseEffortController : ApiController
{
    protected readonly ILogger _logger;

    protected BaseEffortController(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Stores contextual information for exception handling by ApiExceptionFilter.
    /// Call this before throwing exceptions to provide additional context for logging and debugging.
    /// </summary>
    /// <param name="contextInfo">Dictionary of key-value pairs providing context about the operation that failed.</param>
    protected void SetExceptionContext(Dictionary<string, object> contextInfo)
    {
        HttpContext.Items["ExceptionContext"] = contextInfo;
    }

    /// <summary>
    /// Convenience method to set single context property for exception handling.
    /// </summary>
    /// <param name="key">The context property name.</param>
    /// <param name="value">The context property value.</param>
    protected void SetExceptionContext(string key, object value)
    {
        SetExceptionContext(new Dictionary<string, object> { [key] = value });
    }

    /// <summary>
    /// Validates that the request Origin header (if present) matches the request's host.
    /// This prevents CSRF attacks on SSE endpoints that cannot use traditional anti-forgery tokens.
    /// Returns true if the request is same-origin or if no Origin header is present (same-origin requests).
    /// </summary>
    protected bool ValidateSameOrigin()
    {
        var origin = Request.Headers.Origin.FirstOrDefault();

        // No Origin header typically means same-origin navigation or non-browser request
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        // Parse the Origin and compare full origin (scheme + host + port) to the request
        if (Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            var requestScheme = Request.Scheme;
            var requestHost = Request.Host;
            var expectedPort = requestHost.Port ?? (string.Equals(requestScheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80);

            if (string.Equals(originUri.Scheme, requestScheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(originUri.Host, requestHost.Host, StringComparison.OrdinalIgnoreCase)
                && originUri.Port == expectedPort)
            {
                return true;
            }

            _logger.LogWarning(
                "Cross-origin request blocked: Origin={Origin}, RequestHost={RequestHost}",
                LogSanitizer.SanitizeString(origin), LogSanitizer.SanitizeString(requestHost.ToString()));
        }

        return false;
    }
}
