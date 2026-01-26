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
    private readonly VIPERContext _viperContext;
    private readonly IUserHelper _userHelper;
    private int? _cachedPersonId;

    public EffortPermissionService(
        EffortDbContext context,
        RAPSContext rapsContext,
        VIPERContext viperContext,
        IUserHelper? userHelper = null)
    {
        _context = context;
        _rapsContext = rapsContext;
        _viperContext = viperContext;
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
        if (_cachedPersonId.HasValue)
        {
            return _cachedPersonId.Value;
        }

        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            _cachedPersonId = 0;
            return 0;
        }

        // Look up VIPER PersonId via MothraId.
        // Filter for current records only to avoid identity misattribution from historical/inactive records.
        // SingleOrDefault enforces uniqueness - throws if duplicates exist, surfacing data integrity issues.
        var person = _viperContext.People
            .AsNoTracking()
            .Where(p => p.MothraId == user.MothraId && p.Current == 1)
            .SingleOrDefault();

        _cachedPersonId = person?.PersonId ?? 0;
        return _cachedPersonId.Value;
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
}
