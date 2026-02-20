namespace Viper.Areas.Effort.Constants;

/// <summary>
/// Audit action constants for the Effort system.
/// Preserves legacy action names for historical data compatibility.
/// </summary>
public static class EffortAuditActions
{
    // Effort Record Actions
    public const string CreateEffort = "CreateEffort";
    public const string UpdateEffort = "UpdateEffort";
    public const string DeleteEffort = "DeleteEffort";

    // Evaluation Actions
    public const string CreateEval = "CreateEval";
    public const string UpdateEval = "UpdateEval";
    public const string DeleteEval = "DeleteEval";

    // Person/Instructor Actions
    public const string CreatePerson = "CreatePerson";
    public const string UpdatePerson = "UpdatePerson";
    public const string DeleteInstructor = "DeleteInstructor";
    public const string VerifiedEffort = "VerifiedEffort";
    public const string VerifyEmail = "VerifyEmail";

    // Percent Assignment Actions
    public const string CreatePercent = "CreatePercent";
    public const string UpdatePercent = "UpdatePercent";
    public const string DeletePercent = "DeletePercent";

    // Course Actions
    public const string CreateCourse = "CreateCourse";
    public const string UpdateCourse = "UpdateCourse";
    public const string DeleteCourse = "DeleteCourse";
    public const string CreateCourseRelationship = "CreateCourseRelationship";
    public const string DeleteCourseRelationship = "DeleteCourseRelationship";

    // Term Actions
    public const string CreateTerm = "CreateTerm";
    public const string DeleteTerm = "DeleteTerm";
    public const string OpenTerm = "OpenTerm";
    public const string CloseTerm = "CloseTerm";
    public const string ReopenTerm = "ReopenTerm";
    public const string UnopenTerm = "UnopenTerm";
    public const string HarvestTerm = "HarvestTerm";

    // Unit Actions
    public const string CreateUnit = "CreateUnit";
    public const string UpdateUnit = "UpdateUnit";
    public const string DeleteUnit = "DeleteUnit";

    // Effort Type Actions
    public const string CreateEffortType = "CreateEffortType";
    public const string UpdateEffortType = "UpdateEffortType";
    public const string DeleteEffortType = "DeleteEffortType";

    // System-generated actions (hidden from chairs)
    public const string ImportEffort = "ImportEffort";
    public const string RCourseAutoCreated = "RCourseAutoCreated";
    public const string RCourseAutoDeleted = "RCourseAutoDeleted";
    public const string RolloverPercentAssignments = "RolloverPercentAssignments";
    public const string ImportClinical = "ImportClinical";

    // Harvest-specific actions for audit entries created during data import
    public const string HarvestCreatePerson = "HarvestCreatePerson";
    public const string HarvestCreateCourse = "HarvestCreateCourse";
    public const string HarvestCreateEffort = "HarvestCreateEffort";

    /// <summary>
    /// Actions hidden from department-level users (chairs).
    /// Includes harvest/import operations and system-generated automatic entries.
    /// </summary>
    public static readonly string[] HiddenFromChairsActions =
    {
        ImportEffort,
        HarvestTerm,
        RCourseAutoCreated,
        RCourseAutoDeleted,
        RolloverPercentAssignments,
        ImportClinical,
        HarvestCreatePerson,
        HarvestCreateCourse,
        HarvestCreateEffort
    };
}

/// <summary>
/// Table name constants for audit entries.
/// </summary>
public static class EffortAuditTables
{
    public const string Percentages = "Percentages";
    public const string Records = "Records";
    public const string Persons = "Persons";
    public const string Courses = "Courses";
    public const string Terms = "Terms";
    public const string Units = "Units";
    public const string EffortTypes = "EffortTypes";
}
