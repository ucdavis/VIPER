namespace Viper.Areas.Effort.Services;

/// <summary>
/// Shared PDF layout settings for effort report generation (QuestPDF).
/// Compact mode activates when the number of effort type columns exceeds a report-specific threshold.
/// </summary>
public static class ReportPdfSettings
{
    // Font sizes (matching legacy ~9.6pt from 0.8em × 12pt base)
    public const float FontSize = 10f;
    public const float FontSizeCompact = 9f;
    public const float HeaderFontSize = 9f;
    public const float HeaderFontSizeCompact = 8f;

    // Cell vertical padding
    public const float CellPadV = 2f;
    public const float CellPadVCompact = 1.5f;

    // Effort column widths
    public const float EffortWidth = 32f;
    public const float EffortWidthCompact = 24f;

    // Horizontal page margin (inches)
    public const float HMargin = 0.5f;
    public const float HMarginCompact = 0.35f;

    // Extra right padding after spacer columns (VAR, EXM)
    public const float SpacerPad = 10f;
}
