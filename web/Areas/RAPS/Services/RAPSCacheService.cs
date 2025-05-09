using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.RAPS.Services
{
    public class RAPSCacheService
    {
        private readonly RAPSContext rapsContext;
        private readonly AAUDContext aaudContext;
        private readonly IUserHelper userHelper;
        public RAPSCacheService(RAPSContext rapsContext, AAUDContext aaudContext, IUserHelper? userHelper = null)
        {
            this.rapsContext = rapsContext;
            this.aaudContext = aaudContext;
            this.userHelper = userHelper ?? new UserHelper();
        }


        public void ClearCachedRolesAndPermissionsForUser(string mothraId)
        {
            AaudUser? user = aaudContext.AaudUsers.Where(u => u.MothraId == mothraId).FirstOrDefault();
            if (user != null)
            {
                userHelper.ClearCachedRolesAndPermissions(user);
            }
        }
    }

}
