using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>The signed-in user, as the report runner needs them: who they are and what they may do.</summary>
public interface IReportUserService
{
    /// <summary>The signed-in user's identifiers, or null when no one is signed in.</summary>
    ReportCurrentUser? GetCurrentUser();

    /// <summary>Whether the signed-in user has a RAPS permission.</summary>
    bool HasPermission(string permission);
}

public sealed class ReportUserService : IReportUserService
{
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;

    public ReportUserService(RAPSContext rapsContext, IUserHelper userHelper)
    {
        _rapsContext = rapsContext;
        _userHelper = userHelper;
    }

    public ReportCurrentUser? GetCurrentUser()
    {
        AaudUser? user = _userHelper.GetCurrentUser();
        return user is null ? null : new ReportCurrentUser { LoginId = user.LoginId, MothraId = user.MothraId };
    }

    public bool HasPermission(string permission)
    {
        AaudUser? user = _userHelper.GetCurrentUser();
        return user is not null && _userHelper.HasPermission(_rapsContext, user, permission);
    }
}
