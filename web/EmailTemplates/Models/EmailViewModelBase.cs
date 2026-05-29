namespace Viper.EmailTemplates.Models;

/// <summary>
/// Base class for all email view models with common properties.
/// </summary>
public abstract class EmailViewModelBase
{
    /// <summary>
    /// Application base URL for generating absolute links.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Current year for copyright notices in footer.
    /// </summary>
    public int CurrentYear { get; } = DateTime.Now.Year;

    /// <summary>
    /// Helper to generate absolute URLs from relative paths.
    /// </summary>
    /// <param name="relativePath">Relative path starting with /</param>
    /// <returns>Absolute URL combining BaseUrl and relativePath</returns>
    public string GetAbsoluteUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(BaseUrl))
        {
            return relativePath;
        }

        return $"{BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }
}
