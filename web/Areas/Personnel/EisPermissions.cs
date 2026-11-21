namespace Viper.Areas.Personnel;

/// <summary>
/// RAPS permissions for the Employee Information System, named as in the legacy site so the
/// existing role assignments carry over.
/// </summary>
public static class EisPermissions
{
    /// <summary>
    /// Opens EIS. Without <see cref="Department"/> the holder can view every employee.
    /// </summary>
    public const string View = "SVMSecure.EIS";

    /// <summary>
    /// Limits EIS to the employees the holder's units pay, as listed for their login in
    /// AcademicPersonnel's psaDepartmentList. It narrows access even when combined with
    /// <see cref="View"/>, as it did in the legacy site.
    /// </summary>
    public const string Department = "SVMSecure.EIS.dept";

    /// <summary>
    /// Sets and clears the manual appointment categories (Branch Chief, Director, Service Chief
    /// and so on) for the current academic year.
    /// </summary>
    public const string Admin = "SVMSecure.EIS.admin";
}
