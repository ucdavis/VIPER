namespace Viper.Areas.Reports.Engine;

/// <summary>
/// What a catalog needs to list a report: its key, display text and the permissions that grant it.
/// </summary>
public interface IReportIdentity
{
    string Key { get; }

    string Title { get; }

    string Area { get; }

    string Description { get; }

    /// <summary>
    /// The user needs any one of these permissions to run the report.
    /// </summary>
    IReadOnlyList<string> Permissions { get; }
}

/// <summary>
/// A report listed in its area's catalog before it is built, so the area's pages can link to it
/// ahead of time. A planned report can't be described, run or exported: the engine treats those
/// requests as it would an unknown key. When the report's definition is added, delete its planned
/// entry; the registry refuses a key used by both.
/// </summary>
public sealed record PlannedReport(
    string Key, string Title, string Area, string Description, IReadOnlyList<string> Permissions) : IReportIdentity;

/// <summary>
/// Supplies an area's planned reports. <see cref="ReportsServiceCollectionExtensions.AddReports"/>
/// registers every implementation in the report assembly.
/// </summary>
public interface IPlannedReportSource
{
    IReadOnlyList<PlannedReport> PlannedReports { get; }
}
