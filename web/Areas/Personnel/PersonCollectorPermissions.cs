namespace Viper.Areas.Personnel;

/// <summary>
/// RAPS permissions for the Person Collector, named as in the legacy site so the existing role
/// assignments carry over. Every holder sees names and email addresses; the other two add columns.
/// </summary>
public static class PersonCollectorPermissions
{
    /// <summary>Opens the Person Collector.</summary>
    public const string Access = "SVMSecure.Personnel.PersonCollector";

    /// <summary>Adds the login ID column.</summary>
    public const string LoginIds = "SVMSecure.Personnel.PersonCollector.JanKaren";

    /// <summary>Adds the employee ID, MothraID, mail ID, PIDM and Banner ID columns.</summary>
    public const string MoreIds = "SVMSecure.Personnel.PersonCollector.CATS";
}
