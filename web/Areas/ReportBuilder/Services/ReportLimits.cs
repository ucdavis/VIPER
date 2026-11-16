namespace Viper.Areas.ReportBuilder.Services;

/// <summary>How long a report may run and how many rows it may return.</summary>
public static class ReportLimits
{
    /// <summary>A page on screen.</summary>
    public static readonly TimeSpan ScreenTimeout = TimeSpan.FromSeconds(30);

    /// <summary>An Excel or PDF download.</summary>
    public static readonly TimeSpan ExportTimeout = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The most rows a download, an unpaginated report, or a procedure that can't page in SQL
    /// returns. Hitting it marks the result as truncated.
    /// </summary>
    public const int MaxRows = 50_000;

    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 500;

    /// <summary>The most choices a choice parameter's source view may supply.</summary>
    public const int MaxChoices = 1_000;

    /// <summary>The longest text a parameter or search may contain.</summary>
    public const int MaxTextLength = 500;
}
