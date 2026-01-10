namespace Viper.Areas.Effort.Constants;

/// <summary>
/// Permission constants for the Effort system.
/// These match the existing RAPS permissions (verified in VIPER instance).
/// </summary>
public static class EffortPermissions
{
    /// <summary>
    /// Base permission for Effort system access.
    /// </summary>
    public const string Base = "SVMSecure.Effort";

    /// <summary>
    /// View all departments' effort data (admin-level access).
    /// Bypasses department-level filtering.
    /// </summary>
    public const string ViewAllDepartments = "SVMSecure.Effort.ViewAllDepartments";

    /// <summary>
    /// View effort for all courses in the department(s) of the logged in individual.
    /// </summary>
    public const string ViewDept = "SVMSecure.Effort.ViewDept";

    /// <summary>
    /// Create effort records.
    /// </summary>
    public const string CreateEffort = "SVMSecure.Effort.CreateEffort";

    /// <summary>
    /// Delete courses.
    /// </summary>
    public const string DeleteCourse = "SVMSecure.Effort.DeleteCourse";

    /// <summary>
    /// Delete effort records.
    /// </summary>
    public const string DeleteEffort = "SVMSecure.Effort.DeleteEffort";

    /// <summary>
    /// Delete instructor.
    /// </summary>
    public const string DeleteInstructor = "SVMSecure.Effort.DeleteInstructor";

    /// <summary>
    /// Ability to manually add/edit/delete evaluation data from instructors
    /// evaluated outside of CERE for their dept.
    /// </summary>
    public const string EditAdHocEval = "SVMSecure.Effort.EditAdHocEval";

    /// <summary>
    /// Edit courses.
    /// </summary>
    public const string EditCourse = "SVMSecure.Effort.EditCourse";

    /// <summary>
    /// Edit effort records.
    /// </summary>
    public const string EditEffort = "SVMSecure.Effort.EditEffort";

    /// <summary>
    /// Edit the title code, department, percent admin, admin unit, admin title,
    /// and job group ID for an instructor.
    /// </summary>
    public const string EditInstructor = "SVMSecure.Effort.EditInstructor";

    /// <summary>
    /// Users can edit effort data after the term has been closed to editing.
    /// </summary>
    public const string EditWhenClosed = "SVMSecure.Effort.EditWhenClosed";

    /// <summary>
    /// Able to initiate a harvest for a single course.
    /// </summary>
    public const string HarvestCourse = "SVMSecure.Effort.HarvestCourse";

    /// <summary>
    /// Able to initiate a full term harvest.
    /// </summary>
    public const string HarvestTerm = "SVMSecure.Effort.HarvestTerm";

    /// <summary>
    /// Import courses from course catalog.
    /// </summary>
    public const string ImportCourse = "SVMSecure.Effort.ImportCourse";

    /// <summary>
    /// Able to import an instructor into the effort system for a specific term.
    /// </summary>
    public const string ImportInstructor = "SVMSecure.Effort.ImportInstructor";

    /// <summary>
    /// Create relationships for cross-listed and sectioned courses.
    /// </summary>
    public const string LinkCourses = "SVMSecure.Effort.LinkCourses";

    /// <summary>
    /// Ability to assign which units people have access to.
    /// </summary>
    public const string ManageAccess = "SVMSecure.Effort.ManageAccess";

    /// <summary>
    /// Manage R course enrollment.
    /// </summary>
    public const string ManageRCourseEnrollment = "SVMSecure.Effort.ManageRCourseEnrollment";

    /// <summary>
    /// Open/Close the effort system by term code.
    /// </summary>
    public const string ManageTerms = "SVMSecure.Effort.ManageTerms";

    /// <summary>
    /// Manage the list of units which appear in the effort percent dropdowns.
    /// </summary>
    public const string ManageUnits = "SVMSecure.Effort.ManageUnits";

    /// <summary>
    /// Manage the list of effort types which appear in the effort type dropdowns.
    /// </summary>
    public const string ManageEffortTypes = "SVMSecure.Effort.ManageEffortTypes";

    /// <summary>
    /// Access to all reports in the nav.
    /// </summary>
    public const string Reports = "SVMSecure.Effort.Reports";

    /// <summary>
    /// Access to the School Summary Report.
    /// </summary>
    public const string SchoolSummary = "SVMSecure.Effort.SchoolSummary";

    /// <summary>
    /// Confirms the accuracy of a faculty's effort for a term.
    /// </summary>
    public const string VerifyEffort = "SVMSecure.Effort.VerifyEffort";

    /// <summary>
    /// Able to view the audit trail.
    /// </summary>
    public const string ViewAudit = "SVMSecure.Effort.ViewAudit";
}
