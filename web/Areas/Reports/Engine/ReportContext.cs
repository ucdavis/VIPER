namespace Viper.Areas.Reports.Engine;

/// <summary>
/// The user a report runs for. Permission checks go through a delegate so the engine never
/// depends on RAPS directly, and tests can supply any permission set.
/// </summary>
public sealed class ReportContext
{
    private readonly Func<string, bool> _hasPermission;

    public ReportContext(string? loginId, Func<string, bool> hasPermission)
    {
        ArgumentNullException.ThrowIfNull(hasPermission);
        LoginId = loginId;
        _hasPermission = hasPermission;
    }

    public string? LoginId { get; }

    public bool HasPermission(string permission)
    {
        return !string.IsNullOrWhiteSpace(permission) && _hasPermission(permission);
    }

    public bool HasAnyPermission(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        return permissions.Any(HasPermission);
    }
}
