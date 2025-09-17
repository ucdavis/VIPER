using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.ClinicalScheduler.Services;

/// <summary>
/// Implementation of permission validation that encapsulates complex permission logic
/// </summary>
public class PermissionValidator : IPermissionValidator
{
    private readonly ISchedulePermissionService _permissionService;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<PermissionValidator> _logger;

    public PermissionValidator(
        ISchedulePermissionService permissionService,
        RAPSContext rapsContext,
        IUserHelper userHelper,
        ILogger<PermissionValidator> logger)
    {
        _permissionService = permissionService;
        _rapsContext = rapsContext;
        _userHelper = userHelper;
        _logger = logger;
    }

    public async Task<AaudUser> ValidateEditPermissionAndGetUserAsync(int rotationId, string targetMothraId, CancellationToken cancellationToken = default)
    {
        var currentUser = _userHelper.GetCurrentUser();
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("No authenticated user found");
        }

        // First check regular rotation edit permissions
        if (await _permissionService.HasEditPermissionForRotationAsync(rotationId, cancellationToken))
        {
            return currentUser;
        }

        // Check if user has EditOwnSchedule permission and is editing their own schedule
        if (_userHelper.HasPermission(_rapsContext, currentUser, ClinicalSchedulePermissions.EditOwnSchedule) &&
            currentUser.MothraId.Equals(targetMothraId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("User {MothraId} is editing their own schedule with EditOwnSchedule permission", currentUser.MothraId);
            return currentUser;
        }

        throw new UnauthorizedAccessException($"User {currentUser.MothraId} does not have permission to edit rotation {rotationId} or their own schedule (target: {targetMothraId})");
    }
}
