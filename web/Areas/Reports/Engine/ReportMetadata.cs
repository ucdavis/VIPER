using System.Text.Json.Serialization;

namespace Viper.Areas.Reports.Engine;

/// <summary>
/// The kind of input a report parameter renders as on the client.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportParameterType>))]
public enum ReportParameterType
{
    Choice,
    MultiChoice,
    Date,
    Text,
    Number,
    Boolean,
}

/// <summary>
/// How a report column's values are formatted on screen and in exports.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportColumnFormat>))]
public enum ReportColumnFormat
{
    Text,
    Number,
    Currency,
    Percent,
    Date,
    List,
}

/// <summary>
/// Horizontal alignment of a report column.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportAlignment>))]
public enum ReportAlignment
{
    Left,
    Center,
    Right,
}

/// <summary>
/// One selectable value of a choice parameter.
/// </summary>
public sealed record ReportOption(string Value, string Label);

/// <summary>
/// Client-facing description of a report parameter.
/// </summary>
public sealed record ReportParameterMetadata(
    string Name,
    string Label,
    ReportParameterType Type,
    bool Required,
    object? DefaultValue,
    IReadOnlyList<ReportOption> Options);

/// <summary>
/// Client-facing description of a report column.
/// </summary>
public sealed record ReportColumnMetadata(
    string Key,
    string Label,
    ReportColumnFormat Format,
    ReportAlignment Align,
    int? Decimals);

/// <summary>
/// Client-facing description of a report: what it is, what it asks for, and what it shows.
/// </summary>
public sealed record ReportDefinitionMetadata(
    string Key,
    string Title,
    string Area,
    string Description,
    IReadOnlyList<ReportParameterMetadata> Parameters,
    IReadOnlyList<ReportColumnMetadata> Columns);

/// <summary>
/// A problem with the parameters supplied for a report run. <see cref="Parameter"/> is null
/// when the problem involves several parameters rather than one field.
/// </summary>
public sealed record ReportValidationError(string? Parameter, string Message);
