namespace Viper.Areas.Students.Constants;

public static class CareerSelectionPermissions
{
    public const string Admin = "SVMSecure.CareerSelection.Admin";
    public const string Student = "SVMSecure.CareerSelection.Student";
    public const string ReadOnly = "SVMSecure.CareerSelection.ReadOnly";
    public const string ViewOwn = "SVMSecure.CareerSelection.ViewOwn";
    public const string Faculty = "SVMSecure.CareerSelection.Faculty";

    // Comma-separated sets for [Permission(Allow = ...)], which accepts any one of them.

    /// <summary>Those who can fill in a career selection: admins, and students while the app is open.</summary>
    public const string Editors = Admin + "," + Student;

    /// <summary>
    /// Those shown a student roster. Faculty pass this check but see only their mentees, which the
    /// controller narrows further.
    /// </summary>
    public const string StudentListViewers = Admin + "," + ReadOnly + "," + Faculty;

    /// <summary>
    /// Those who may open a career selection record: the roster viewers, plus a student's own
    /// access. Which record each may open is narrowed in the controller, since a mentor's reach
    /// depends on the data rather than on their permissions.
    /// </summary>
    public const string RecordViewers = Admin + "," + ReadOnly + "," + Faculty + "," + Student + "," + ViewOwn;
}
