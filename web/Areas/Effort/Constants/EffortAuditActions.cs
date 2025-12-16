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

    // Import Actions (hidden from chairs)
    public const string ImportEffort = "ImportEffort";

    /// <summary>
    /// Actions that should be hidden from department-level users (chairs).
    /// </summary>
    public static readonly string[] ImportActions = { ImportEffort };
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
}
