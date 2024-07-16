using Viper.Classes.SQLContext;
using Viper.Classes;

namespace Viper.Areas.CTS.Services
{
    public class CtsNavMenu
    {
        private readonly RAPSContext _rapsContext;
        public CtsNavMenu(RAPSContext context)
        {
            _rapsContext = context;
        }
        public NavMenu Nav()
        {
            UserHelper userHelper = new UserHelper();
            
            var nav = new List<NavMenuItem>
            {
                //new NavMenuItem() { MenuItemText = "CTS Home", MenuItemURL = "Home" },
                new NavMenuItem() { MenuItemText = "Assessments", IsHeader = true }
            };

            if (userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.AssessClinical")
                || userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Manage"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "EPA Assessment", MenuItemURL = "EPA" });
            }

            //All assessments, or assessments the logged in userHelper has created
            if (userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Manage")
                || userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.StudentAssessments")
                || userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.AssessClinical"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "View Assessments", MenuItemURL = "AssessmentList" });
            }
            //Assessments of the logged in user
            if (userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Students"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "My Assessments", MenuItemURL = "MyAssessments" });
            }

            if (userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Manage"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Admin Functions", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Domains", MenuItemURL = "ManageDomains" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Competencies", MenuItemURL = "ManageCompetencies" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Levels", MenuItemURL = "ManageLevels" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage EPAs", MenuItemURL = "ManageEPAs" });
                nav.Add(new NavMenuItem() { MenuItemText = "Audit Log", MenuItemURL = "Audit" });

                nav.Add(new NavMenuItem() { MenuItemText = "Reports", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Assessment Charts", MenuItemURL = "AssessmentChart" });
            }

            return new NavMenu("Competency Tracking System", nav);
        }
    }
}
