using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Areas.RAPS.Services;

namespace Viper.Areas.Directory.Models
{
    public class DirectoryUser
    {
        public bool CanDisplayIDs { get; set; } = false;
        public bool CanEmulate { get; set; } = false;
        public bool CanSeeAllStudents { get; set; } = false;
        public bool CanSeeUCPathInfo { get; set; } = false;
        public bool CanSeeAltPhoto { get; set; } = false;

        public DirectoryUser()
        {
            IUserHelper UserHelper = new UserHelper();
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            RAPSContext? _rapsContext = (RAPSContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(RAPSContext));
            this.CanDisplayIDs = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail");
            this.CanEmulate = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.SU");
            this.CanSeeAllStudents = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.SIS.AllStudents");
            this.CanSeeUCPathInfo = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryUCPathInfo");
            this.CanSeeAltPhoto = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.CATS.ServiceDesk");
        }
    }
}