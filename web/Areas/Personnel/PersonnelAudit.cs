namespace Viper.Areas.Personnel;

/// <summary>
/// The logger for the Personnel audit trail: who viewed EIS pages (dates of birth, salaries,
/// home addresses), who changed an appointment category, and who pulled Person Collector lists.
/// nlog.config writes it to its own file at Info, because TEST and PROD log everything else
/// only from Warning up.
/// </summary>
public static class PersonnelAudit
{
    /// <summary>The logger name the nlog.config rule matches; keep the two in step.</summary>
    public const string LoggerName = "PersonnelAudit";
}
