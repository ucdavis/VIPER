using Viper.Classes.SQLContext;
using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel.Services
{
    public class PhonesPermissionsService(
        RAPSContext rapsContext,
        IUserHelper userHelper
    )
    {
        private readonly RAPSContext _rapsContext = rapsContext;
        private readonly IUserHelper _userHelper = userHelper;

        /// <summary>
        /// Whether the caller may edit the given list. The role comes from the list's own
        /// MaintainRole column rather than a hard-coded constant, so a new list is a row in
        /// phones.PhoneList and its role, not a code change.
        ///
        /// Takes the resolved list rather than an id: every caller has already loaded it to route
        /// the request, and an id-based overload would have to fetch the same row a second time.
        /// </summary>
        public bool CanMaintainList(PhoneList list)
        {
            var user = _userHelper.GetCurrentUser();
            if (user == null)
            {
                return false;
            }
            return _userHelper.HasPermission(_rapsContext, user, list.MaintainRole);
        }
    }
}
