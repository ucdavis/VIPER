using Viper.Classes;

namespace Viper.Areas.ClinicalScheduler.Services
{
    public class ClinicalSchedulerNavMenu
    {
        public NavMenu Nav()
        {
            var oldViperUrl = HttpHelper.GetOldViperRootURL();

            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Clinical Scheduler", IsHeader = true },
                new NavMenuItem() { MenuItemText = "Clinical Scheduler 2.0", MenuItemURL = "/ClinicalScheduler/" },
                new NavMenuItem() { MenuItemText = "Clinical Scheduler 1.0", MenuItemURL = $"{oldViperUrl}/clinicalScheduler/" }
            };

            return new NavMenu("Clinical Scheduler", nav);
        }
    }
}
