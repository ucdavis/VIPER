namespace Viper.Areas.ReportBuilder.Models;

/// <summary>What kind of object in the [reports] schema a report reads.</summary>
public enum ReportSourceKind
{
    View,
    Procedure,
}

/// <summary>
/// The kind of value a source column or parameter holds, from its SQL type. Binary and
/// <see cref="Other"/> values can't be shown in a report.
/// </summary>
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
public enum ReportParameterBinding
{
    UserInput,
    CurrentUserLoginId,
    CurrentUserMothraId,
}

/// <summary>What a user did with a report, for the run log.</summary>
public enum ReportRunAction
{
    Run,
    ExportExcel,
    ExportPdf,
}

/// <summary>What happened to a report definition, for its history.</summary>
public enum ReportHistoryAction
{
    Created,
    Updated,
    Deleted,
    Restored,
}
