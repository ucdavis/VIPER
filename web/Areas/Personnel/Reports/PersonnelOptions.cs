using Viper.Areas.Reports.Engine;

namespace Viper.Areas.Personnel.Reports;

/// <summary>
/// Choice lists shared by several Personnel reports, using the codes the PPS procedures expect.
/// </summary>
public static class PersonnelOptions
{
    public const string Faculty = "F";
    public const string Staff = "S";
    public const string Both = "B";
    public const string Senate = "S";
    public const string Federation = "F";

    public static readonly IReadOnlyList<ReportOption> FacultyStaffBoth =
    [
        new(Faculty, "Faculty"),
        new(Staff, "Staff"),
        new(Both, "Both"),
    ];

    public static readonly IReadOnlyList<ReportOption> SenateFederationBoth =
    [
        new(Senate, "Senate"),
        new(Federation, "Federation"),
        new(Both, "Both"),
    ];
}
