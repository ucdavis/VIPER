using System.Text.Json.Serialization;

namespace Viper.Areas.ReportBuilder.Models;

// Every enum is sent to and from the browser by name, as the database stores it.

/// <summary>What kind of object in the [report] schema a report reads.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportSourceKind>))]
public enum ReportSourceKind
{
    View,
    Procedure,
}

/// <summary>
/// The kind of value a source column or parameter holds, from its SQL type. Binary and
/// <see cref="Other"/> values can't be shown in a report.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportValueKind>))]
public enum ReportValueKind
{
    Text,
    WholeNumber,
    FractionalNumber,
    Date,
    DateTime,
    Time,
    Boolean,
    UniqueId,
    Binary,
    Other,
}

/// <summary>How a report column's values are shown on screen and in exports.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportColumnFormat>))]
public enum ReportColumnFormat
{
    Text,
    Number,
    Currency,
    Percent,
    Date,
    DateTime,
    YesNo,
}

/// <summary>The input a report parameter takes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportParameterType>))]
public enum ReportParameterType
{
    Text,
    Number,
    Date,
    Boolean,
    Choice,
    MultiChoice,
}

/// <summary>How a parameter filters a view's column. Procedure parameters have no operator.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportFilterOperator>))]
public enum ReportFilterOperator
{
    Equals,
    Contains,
    From,
    To,
    In,
}

/// <summary>
/// Where a parameter's value comes from. The current-user bindings are filled in on the server
/// and can't be changed by the user, which lets a source return only that user's rows.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportParameterBinding>))]
public enum ReportParameterBinding
{
    UserInput,
    CurrentUserLoginId,
    CurrentUserMothraId,
}

/// <summary>What a user did with a report, for the run log.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportRunAction>))]
public enum ReportRunAction
{
    Run,
    ExportExcel,
    ExportPdf,
}

/// <summary>What happened to a report definition, for its history.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportHistoryAction>))]
public enum ReportHistoryAction
{
    Created,
    Updated,
    Deleted,
    Restored,
}
