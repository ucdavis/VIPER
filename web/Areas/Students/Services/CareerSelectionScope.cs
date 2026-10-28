namespace Viper.Areas.Students.Services;

/// <summary>
/// Which students a caller may see. Resolved once per request from the caller's permissions, so
/// that the precedence between them is decided in one place rather than at each endpoint.
/// </summary>
public enum CareerSelectionScope
{
    /// <summary>No career selection access at all.</summary>
    None,
    /// <summary>Every student. Admin and read-only callers.</summary>
    All,
    /// <summary>Only the students whose career selection names this caller as their mentor.</summary>
    Mentored,
    /// <summary>Only the caller's own record. Students.</summary>
    Own
}
