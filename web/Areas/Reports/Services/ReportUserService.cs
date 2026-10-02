using Viper.Areas.Reports.Engine;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.Reports.Services;

public interface IReportUserService
{
    /// <summary>
    /// The signed-in user as a report context. A request with no resolvable user gets a context
    /// with no permissions, so every report check fails closed.
    /// </summary>
    ReportContext GetCurrentContext();
}

public class ReportUserService : IReportUserService
{
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;

    public ReportUserService(RAPSContext rapsContext, IUserHelper userHelper)
    {
        _rapsContext = rapsContext;
        _userHelper = userHelper;
    }

    public ReportContext GetCurrentContext()
    {
        AaudUser? user = _userHelper.GetCurrentUser();
        if (user is null)
        {
            return new ReportContext(null, _ => false);
        }

        return new ReportContext(user.LoginId, permission => _userHelper.HasPermission(_rapsContext, user, permission));
    }
}
