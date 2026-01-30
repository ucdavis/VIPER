using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for checking Effort system permissions.
/// Implements two-layer permission model:
/// - RAPS permissions (ViewAllDepartments, ViewDept, etc.)
/// - UserAccess table for department filtering
/// </summary>
public class EffortPermissionService : IEffortPermissionService
{
    private readonly EffortDbContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;

    public EffortPermissionService(
        EffortDbContext context,
        RAPSContext rapsContext,
        IUserHelper? userHelper = null)
    {
        _context = context;
        _rapsContext = rapsContext;
        _userHelper = userHelper ?? new UserHelper();
    }

    /// <inheritdoc />
    public Task<bool> HasFullAccessAsync(CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return Task.FromResult(false);
        }

        var hasFullAccess = _userHelper.HasPermission(_rapsContext, user, EffortPermissions.ViewAllDepartments);
        return Task.FromResult(hasFullAccess);
    }

    /// <inheritdoc />
    public Task<bool> HasDepartmentLevelAccessAsync(CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return Task.FromResult(false);
        }

        var hasDeptAccess = _userHelper.HasPermission(_rapsContext, user, EffortPermissions.ViewDept);
        return Task.FromResult(hasDeptAccess);
    }

    /// <inheritdoc />
    public Task<bool> HasSelfServiceAccessAsync(CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return Task.FromResult(false);
        }

        var hasSelfService = _userHelper.HasPermission(_rapsContext, user, EffortPermissions.VerifyEffort);
        return Task.FromResult(hasSelfService);
    }

    /// <inheritdoc />
    public async Task<bool> CanViewDepartmentAsync(string departmentCode, CancellationToken ct = default)
    {
        if (await HasFullAccessAsync(ct))
        {
            return true;
        }

        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return false;
        }

        if (!_userHelper.HasPermission(_rapsContext, user, EffortPermissions.ViewDept))
        {
            return false;
        }

        var authorizedDepts = await GetAuthorizedDepartmentsAsync(ct);
        return authorizedDepts.Contains(departmentCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<bool> CanEditDepartmentAsync(string departmentCode, CancellationToken ct = default)
    {
        if (await HasFullAccessAsync(ct))
        {
            return true;
        }

        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return false;
        }

        if (!_userHelper.HasPermission(_rapsContext, user, EffortPermissions.EditEffort))
        {
            return false;
        }

        var authorizedDepts = await GetAuthorizedDepartmentsAsync(ct);
        return authorizedDepts.Contains(departmentCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<bool> CanVerifyDepartmentAsync(string departmentCode, CancellationToken ct = default)
    {
        return CanViewDepartmentAsync(departmentCode, ct);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetAuthorizedDepartmentsAsync(CancellationToken ct = default)
    {
        if (await HasFullAccessAsync(ct))
        {
            return new List<string>();
        }

        var personId = GetCurrentPersonId();
        if (personId == 0)
        {
            return new List<string>();
        }

        return await _context.UserAccess
            .AsNoTracking()
            .Where(ua => ua.PersonId == personId && ua.IsActive)
            .Select(ua => ua.DepartmentCode)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanViewPersonEffortAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return false;
        }

        if (IsCurrentUser(personId) && _userHelper.HasPermission(_rapsContext, user, EffortPermissions.VerifyEffort))
        {
            return true;
        }

        if (await HasFullAccessAsync(ct))
        {
            return true;
        }

        if (!_userHelper.HasPermission(_rapsContext, user, EffortPermissions.ViewDept))
        {
            return false;
        }

        var person = await _context.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId && p.TermCode == termCode, ct);

        if (person == null)
        {
            return false;
        }

        var authorizedDepts = await GetAuthorizedDepartmentsAsync(ct);

        return authorizedDepts.Contains(person.EffortDept, StringComparer.OrdinalIgnoreCase) ||
               (!string.IsNullOrEmpty(person.ReportUnit) &&
                authorizedDepts.Contains(person.ReportUnit, StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<bool> CanEditPersonEffortAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return false;
        }

        if (await HasFullAccessAsync(ct))
        {
            return true;
        }

        if (!_userHelper.HasPermission(_rapsContext, user, EffortPermissions.EditEffort))
        {
            return false;
        }

        var person = await _context.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId && p.TermCode == termCode, ct);

        if (person == null)
        {
            return false;
        }

        var authorizedDepts = await GetAuthorizedDepartmentsAsync(ct);

        return authorizedDepts.Contains(person.EffortDept, StringComparer.OrdinalIgnoreCase) ||
               (!string.IsNullOrEmpty(person.ReportUnit) &&
                authorizedDepts.Contains(person.ReportUnit, StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public int GetCurrentPersonId()
    {
        var user = _userHelper.GetCurrentUser();
        return user?.AaudUserId ?? 0;
    }

    /// <inheritdoc />
    public bool IsCurrentUser(int personId)
    {
        return GetCurrentPersonId() == personId && personId != 0;
    }

    /// <inheritdoc />
    public Task<bool> HasAnyViewAccessAsync(CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return Task.FromResult(false);
        }

        var hasFullAccess = _userHelper.HasPermission(_rapsContext, user, EffortPermissions.ViewAllDepartments);
        var hasDeptAccess = _userHelper.HasPermission(_rapsContext, user, EffortPermissions.ViewDept);
        var hasSelfService = _userHelper.HasPermission(_rapsContext, user, EffortPermissions.VerifyEffort);

        return Task.FromResult(hasFullAccess || hasDeptAccess || hasSelfService);
    }

    /// <inheritdoc />
    public string? GetCurrentUserEmail()
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null || string.IsNullOrWhiteSpace(user.MailId))
        {
            return null;
        }

        return $"{user.MailId.Trim()}@ucdavis.edu";
    }

    /// <inheritdoc />
    public async Task<bool> CanViewPersonPercentagesAsync(int personId, CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return false;
        }

        if (await HasFullAccessAsync(ct))
        {
            return true;
        }

        if (!_userHelper.HasPermission(_rapsContext, user, EffortPermissions.ViewDept))
        {
            return false;
        }

        var authorizedDepts = await GetAuthorizedDepartmentsAsync(ct);

        // Check if the person exists in any term with a department the user can access
        return await _context.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .AnyAsync(p => authorizedDepts.Contains(p.EffortDept) ||
                          (!string.IsNullOrEmpty(p.ReportUnit) && authorizedDepts.Contains(p.ReportUnit)), ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanEditPersonPercentagesAsync(int personId, CancellationToken ct = default)
    {
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return false;
        }

        if (await HasFullAccessAsync(ct))
        {
            return true;
        }

        if (!_userHelper.HasPermission(_rapsContext, user, EffortPermissions.EditEffort))
        {
            return false;
        }

        var authorizedDepts = await GetAuthorizedDepartmentsAsync(ct);

        // Check if the person exists in any term with a department the user can access
        return await _context.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .AnyAsync(p => authorizedDepts.Contains(p.EffortDept) ||
                          (!string.IsNullOrEmpty(p.ReportUnit) && authorizedDepts.Contains(p.ReportUnit)), ct);
    }
}
