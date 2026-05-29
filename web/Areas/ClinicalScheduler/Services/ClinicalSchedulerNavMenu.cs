using Viper.Classes;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    public class ClinicalSchedulerNavMenu
    {
        private readonly RAPSContext _rapsContext;

        public ClinicalSchedulerNavMenu(RAPSContext context)
        {
            _rapsContext = context;
        }

        public NavMenu Nav()
        {
            var oldViperUrl = HttpHelper.GetOldViperRootURL();
            var userHelper = new UserHelper();
            var user = userHelper.GetCurrentUser();
            var hasAccess = userHelper.HasPermission(_rapsContext, user, "SVMSecure.ClnSched");

            var nav = new List<NavMenuItem>
            {
                new NavMenuItem { MenuItemText = "Clinical Scheduler", IsHeader = true }
            };

            if (hasAccess)
            {
                nav.Add(new NavMenuItem { MenuItemText = "Clinical Scheduler 2.0", MenuItemURL = "~/ClinicalScheduler/" });
                nav.Add(new NavMenuItem { MenuItemText = "Schedule by Rotation", MenuItemURL = "~/ClinicalScheduler/rotation", IndentLevel = 1 });
                nav.Add(new NavMenuItem { MenuItemText = "Schedule by Clinician", MenuItemURL = "~/ClinicalScheduler/clinician", IndentLevel = 1 });
                nav.Add(new NavMenuItem { MenuItemText = "Clinical Scheduler 1.0", MenuItemURL = $"{oldViperUrl}/clinicalScheduler/" });
            }

            return new NavMenu("Clinical Scheduler", nav);
        }
    }
}
