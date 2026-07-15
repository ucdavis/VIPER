using Viper.Classes;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    public class ClinicalSchedulerNavMenu
    {
        private readonly RAPSContext _rapsContext;
        private readonly ISchedulePermissionService _permissionService;

        public ClinicalSchedulerNavMenu(RAPSContext context, ISchedulePermissionService permissionService)
        {
            _rapsContext = context;
            _permissionService = permissionService;
        }

        public async Task<NavMenu> Nav(CancellationToken cancellationToken = default)
        {
            var oldViperUrl = HttpHelper.GetOldViperRootURL();
            var userHelper = new UserHelper();
            var user = userHelper.GetCurrentUser();
            var hasAccess = userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Base);
            var hasManage = userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Manage);

            var nav = new List<NavMenuItem>
            {
                new NavMenuItem { MenuItemText = "Clinical Scheduler", IsHeader = true }
            };

            if (hasAccess)
            {
                nav.Add(new NavMenuItem { MenuItemText = "Clinical Scheduler 2.0", MenuItemURL = "~/ClinicalScheduler/" });

                // Only show the sub-views the user can actually open. These checks mirror the
                // client-side canAccessRotationView / canAccessClinicianView getters so the nav
                // does not advertise a view that the Vue app will reject with "Access Denied".
                if (await _permissionService.CanAccessRotationViewAsync(cancellationToken))
                {
                    nav.Add(new NavMenuItem { MenuItemText = "Schedule by Rotation", MenuItemURL = "~/ClinicalScheduler/rotation", IndentLevel = 1 });
                }

                if (await _permissionService.CanAccessClinicianViewAsync(cancellationToken))
                {
                    var clinicianLabel = await _permissionService.GetClinicianViewLabelAsync(cancellationToken);
                    nav.Add(new NavMenuItem { MenuItemText = clinicianLabel, MenuItemURL = "~/ClinicalScheduler/clinician", IndentLevel = 1 });
                }

                if (hasManage)
                {
                    nav.Add(new NavMenuItem { MenuItemText = "Audit Trail", MenuItemURL = "~/ClinicalScheduler/audit", IndentLevel = 1 });
                }


                nav.Add(new NavMenuItem { MenuItemText = "Clinical Scheduler 1.0", MenuItemURL = $"{oldViperUrl}/clinicalScheduler/" });
            }

            return new NavMenu("Clinical Scheduler", nav);
        }
    }
}
