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
                nav.Add(new NavMenuItem() { MenuItemText = "EPA Assessment", MenuItemURL = "https://ucdsvm.knowledgeowl.com/help/epa-assessments" });
            }
            if (userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.AssessStudent")
                && userHelper.HasPermission(_rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Manage"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Competency Assessment (Wireframe)", MenuItemURL = "AssessmentCompetency" });
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
                nav.Add(new NavMenuItem() { MenuItemText = "Courses", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Course Students (Wireframe)", MenuItemURL = "CourseStudents" });
                nav.Add(new NavMenuItem() { MenuItemText = "Course Competencies", MenuItemURL = "ManageCourseCompetencies" });
                nav.Add(new NavMenuItem() { MenuItemText = "Legacy Competency Mapping", MenuItemURL = "ManageLegacyCompetencyMapping" });

                nav.Add(new NavMenuItem() { MenuItemText = "Admin Functions", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Domains", MenuItemURL = "ManageDomains" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Competencies", MenuItemURL = "ManageCompetencies" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Bundles", MenuItemURL = "ManageBundles" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Levels", MenuItemURL = "ManageLevels" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage EPAs", MenuItemURL = "ManageEPAs" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Milestones", MenuItemURL = "ManageMilestones" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Roles", MenuItemURL = "ManageRoles" });
                nav.Add(new NavMenuItem() { MenuItemText = "Audit Log", MenuItemURL = "Audit" });

                //nav.Add(new NavMenuItem() { MenuItemText = "Reports", IsHeader = true });
                //nav.Add(new NavMenuItem() { MenuItemText = "Assessment Charts", MenuItemURL = "AssessmentChart" });
            }

            return new NavMenu("Competency Tracking System", nav);
        }
    }
}
