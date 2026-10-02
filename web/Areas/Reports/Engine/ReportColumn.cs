namespace Viper.Areas.Reports.Engine;

/// <summary>
/// One column of a report, with the function that reads its value from a row.
/// </summary>
internal sealed class ReportColumn<TRow>
{
    public ReportColumn(string key, string label, Func<TRow, object?> value)
    {
        Key = key;
        Label = label;
        Value = value;
    }

    public string Key { get; }

    public string Label { get; }

    public Func<TRow, object?> Value { get; }

    // Defaults are the enums' first members: Text, aligned left.
    public ReportColumnFormat Format { get; set; }

    public ReportAlignment Align { get; set; }

    public int? Decimals { get; set; }

    public Func<ReportContext, bool>? VisibleWhen { get; set; }

    public bool IsVisible(ReportContext context)
    {
        return VisibleWhen?.Invoke(context) ?? true;
    }

    public ReportColumnMetadata ToMetadata()
    {
        return new ReportColumnMetadata(Key, Label, Format, Align, Decimals);
    }
}

/// <summary>
/// Fluent options for a column declared on a <see cref="ReportBuilder{TRow, TParams}"/>.
/// </summary>
public sealed class ReportColumnBuilder<TRow>
{
    private const int CurrencyDecimals = 2;

    private readonly ReportColumn<TRow> _column;

    internal ReportColumnBuilder(ReportColumn<TRow> column)
    {
        _column = column;
    }

    public ReportColumnBuilder<TRow> AsNumber(int decimals = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);
        return SetFormat(ReportColumnFormat.Number, ReportAlignment.Right, decimals);
    }

    public ReportColumnBuilder<TRow> AsCurrency()
    {
        return SetFormat(ReportColumnFormat.Currency, ReportAlignment.Right, CurrencyDecimals);
    }

    public ReportColumnBuilder<TRow> AsPercent(int decimals = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);
        return SetFormat(ReportColumnFormat.Percent, ReportAlignment.Right, decimals);
    }

    public ReportColumnBuilder<TRow> AsDate()
    {
        return SetFormat(ReportColumnFormat.Date, ReportAlignment.Center, null);
    }

    public ReportColumnBuilder<TRow> AsList()
    {
        return SetFormat(ReportColumnFormat.List, ReportAlignment.Left, null);
    }

    public ReportColumnBuilder<TRow> Align(ReportAlignment align)
    {
        _column.Align = align;
        return this;
    }

    /// <summary>
    /// Shows the column only to users the predicate accepts, for example columns of
    /// identifiers that only some roles may see.
    /// </summary>
    public ReportColumnBuilder<TRow> VisibleWhen(Func<ReportContext, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _column.VisibleWhen = predicate;
        return this;
    }

    private ReportColumnBuilder<TRow> SetFormat(ReportColumnFormat format, ReportAlignment align, int? decimals)
    {
        _column.Format = format;
        _column.Align = align;
        _column.Decimals = decimals;
        return this;
    }
}
