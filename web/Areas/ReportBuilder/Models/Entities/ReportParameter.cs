namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>
/// One report parameter (<c>report.report_parameter</c>): a filter on a view column, or a value
/// for a procedure parameter.
/// </summary>
public class ReportParameter
{
    public int Id { get; set; }
    public int ReportId { get; set; }

    /// <summary>The view column to filter, or the procedure parameter name without the @.</summary>
    public string? Name { get; set; }

    public string? Label { get; set; }
    public int? DisplayOrder { get; set; }
    public ReportParameterType? Type { get; set; }

    /// <summary>How a view column is filtered; null for procedure parameters.</summary>
    public ReportFilterOperator? Operator { get; set; }

    public bool? Required { get; set; }
    public string? DefaultValue { get; set; }

    /// <summary>A [report] view returning value and label columns, for choice parameters.</summary>
    public string? ChoicesSource { get; set; }

    /// <summary>A fixed list of choices as JSON, for choice parameters without a view.</summary>
    public string? ChoicesList { get; set; }

    public ReportParameterBinding? Binding { get; set; }
    public ReportDefinition? Report { get; set; }
}
