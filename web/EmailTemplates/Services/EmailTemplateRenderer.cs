using Razor.Templating.Core;

namespace Viper.EmailTemplates.Services;

/// <summary>
/// Renders Razor email templates to HTML strings using Razor.Templating.Core.
/// Works without HttpContext, suitable for service layer and background jobs.
/// </summary>
public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly ILogger<EmailTemplateRenderer> _logger;

    public EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> RenderAsync<TModel>(
        string templatePath,
        TModel model,
        Dictionary<string, object>? viewData = null)
    {
        try
        {
            var html = await RazorTemplateEngine.RenderAsync(templatePath, model, viewData);
            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render email template {TemplatePath}", templatePath);
            throw new InvalidOperationException($"Failed to render email template: {templatePath}", ex);
        }
    }
}
