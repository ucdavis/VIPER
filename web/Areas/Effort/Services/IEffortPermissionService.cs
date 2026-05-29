namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for checking Effort system permissions.
/// Implements two-layer permission model: RAPS permissions + UserAccess table.
/// </summary>
public interface IEffortPermissionService
{
    /// <summary>
    /// Check if current user has full access (Admin or Manage permission).
    /// Full access bypasses all department filtering.
    /// </summary>
    Task<bool> HasFullAccessAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if current user has department-level access (ViewDept, EditDept, or VerifyDept).
    /// </summary>
    Task<bool> HasDepartmentLevelAccessAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if current user has self-service access (ViewOwn or EditOwn).
    /// </summary>
    Task<bool> HasSelfServiceAccessAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if current user can view a specific department's data.
    /// Full access users can view all departments.
    /// Department-level users must have the department in their UserAccess entries.
    /// </summary>
    Task<bool> CanViewDepartmentAsync(string departmentCode, CancellationToken ct = default);

    /// <summary>
    /// Check if current user can edit a specific department's data.
    /// </summary>
    Task<bool> CanEditDepartmentAsync(string departmentCode, CancellationToken ct = default);

    /// <summary>
    /// Check if current user can verify/approve effort for a specific department.
    /// </summary>
    Task<bool> CanVerifyDepartmentAsync(string departmentCode, CancellationToken ct = default);

    /// <summary>
    /// Get list of department codes the current user is authorized to access.
    /// Returns empty list for full-access users (meaning all departments).
    /// </summary>
    Task<List<string>> GetAuthorizedDepartmentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if current user can view effort data for a specific person in a term.
    /// Checks self-service (ViewOwn), full access, or department-level access.
    /// </summary>
    Task<bool> CanViewPersonEffortAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Check if current user can edit effort data for a specific person in a term.
    /// Checks self-service (EditOwn), full access, or department-level access.
    /// </summary>
    Task<bool> CanEditPersonEffortAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get the current user's PersonId from AAUD.
    /// Returns 0 if not authenticated.
    /// </summary>
    int GetCurrentPersonId();

    /// <summary>
    /// Check if the specified PersonId matches the current user.
    /// </summary>
    bool IsCurrentUser(int personId);

    /// <summary>
    /// Check if current user has any view-level access permission
    /// (ViewAllDepartments, ViewDept, or VerifyEffort).
    /// </summary>
    Task<bool> HasAnyViewAccessAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the current user's email address (mailId@ucdavis.edu).
    /// Returns null if not authenticated or no email address found.
    /// </summary>
    string? GetCurrentUserEmail();

    /// <summary>
    /// Check if current user can view percentage assignments for a person.
    /// Unlike CanViewPersonEffortAsync, this doesn't require a specific term -
    /// it checks if the person exists in any term the user has access to.
    /// Requires ViewAllDepartments or ViewDept permission (no self-service access).
    /// </summary>
    Task<bool> CanViewPersonPercentagesAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Check if current user can edit percentage assignments for a person.
    /// Unlike CanEditPersonEffortAsync, this doesn't require a specific term -
    /// it checks if the person exists in any term the user has access to.
    /// </summary>
    Task<bool> CanEditPersonPercentagesAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Check if a term is editable (term is open OR user has EditWhenClosed permission).
    /// </summary>
    Task<bool> IsTermEditableAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Check if the current user can edit their OWN effort only.
    /// This is a SELF-EDIT check - requires VerifyEffort permission AND being the record owner.
    /// </summary>
    Task<bool> CanEditOwnEffortAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Check if the current user has a specific RAPS permission.
    /// </summary>
    Task<bool> HasPermissionAsync(string permission, CancellationToken ct = default);

    /// <summary>
    /// Get the current user's department code for audit filtering.
    /// Users with ViewDeptAudit can only see audit entries for their own department.
    /// Returns the EffortDept from the most recent term record for the current user.
    /// </summary>
    Task<string?> GetUserDepartmentAsync(CancellationToken ct = default);
}
