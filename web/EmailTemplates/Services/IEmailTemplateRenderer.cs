namespace Viper.EmailTemplates.Services;

/// <summary>
/// Renders Razor email templates to HTML strings without requiring HttpContext.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders an email template to an HTML string.
    /// </summary>
    /// <typeparam name="TModel">The view model type.</typeparam>
    /// <param name="templatePath">Path to the template (e.g., "/Areas/Effort/EmailTemplates/Views/VerificationReminder.cshtml").</param>
    /// <param name="model">The strongly-typed view model.</param>
    /// <param name="viewData">Optional ViewData dictionary for layout/title.</param>
    /// <returns>Rendered HTML string.</returns>
    Task<string> RenderAsync<TModel>(string templatePath, TModel model, Dictionary<string, object>? viewData = null);
}
