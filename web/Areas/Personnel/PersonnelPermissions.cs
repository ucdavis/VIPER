namespace Viper.Areas.Personnel;

/// <summary>
/// RAPS permissions used by the Personnel section, named as they are in the legacy site so the
/// existing role assignments carry over unchanged. Add each one when the page or report that
/// needs it is ported.
/// </summary>
public static class PersonnelPermissions
{
    public const string FacultySalary = "SVMSecure.Personnel.FacultySalary";
    public const string Sabbatic = "SVMSecure.Personnel.Sabbatic";
    public const string ServiceCreditAdmin = "SVMSecure.Personnel.ServiceCreditAdmin";
}
