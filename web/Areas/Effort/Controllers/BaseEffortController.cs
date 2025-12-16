using Viper.Classes;

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
}
