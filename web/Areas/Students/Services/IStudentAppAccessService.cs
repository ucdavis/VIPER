namespace Viper.Areas.Students.Services;

/// <summary>
/// Opens and closes a student self-service app, such as emergency contacts or career selection.
/// An app is open while the DVM student role holds its student permission, so each method takes
/// that permission's name.
/// </summary>
public interface IStudentAppAccessService
{
    /// <summary>
    /// Whether students can currently reach the app. True only while the DVM student role holds
    /// the permission and the row grants rather than denies it.
    /// </summary>
    Task<bool> IsAppOpenAsync(string studentPermission);

    /// <summary>
    /// Opens the app if it is closed and closes it if it is open. Returns whether it is now open.
    /// </summary>
    Task<bool> ToggleAppAccessAsync(string studentPermission);

    /// <summary>
    /// The RAPS id of a permission, for callers that grant or revoke it themselves. Throws if the
    /// permission does not exist: the names are compile-time constants, so a miss means the RAPS
    /// row is missing rather than that the caller asked for something optional.
    /// </summary>
    Task<int> GetPermissionIdAsync(string permissionName);
}
