namespace Viper.Areas.ReportBuilder;

/// <summary>
/// RAPS permissions for the Report Builder itself. Who may view each report is stored with the
/// report, as one or more ordinary RAPS permission names.
/// </summary>
public static class ReportBuilderPermissions
{
    /// <summary>Create, edit and delete report definitions.</summary>
    public const string Admin = "SVMSecure.Reports.Admin";
}
