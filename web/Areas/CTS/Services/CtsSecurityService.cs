using Viper.Classes.SQLContext;

namespace Viper.Areas.CTS.Services
{
    public class CtsSecurityService
    {
        private readonly RAPSContext rapsContext;
        private readonly VIPERContext viperContext;
        public IUserHelper userHelper;

        private const string ManagerPermission = "SVMSecure.CTS.Manage";
        private const string AssessmentsViewPermission = "SVMSecure.CTS.ViewAllStudentAssessments";
        private const string AssessClinicalPermission = "SVMSecure.CTS.AssessClinical";


        public CtsSecurityService(RAPSContext rapsContext, VIPERContext viperContext, IUserHelper? userHelper = null)
        {
            this.rapsContext = rapsContext;
            this.viperContext = viperContext;
            this.userHelper = userHelper ?? new UserHelper();
        }

        /// <summary>
        /// Check parameters for viewing student assessments
        ///     Students can view their own assessments
        ///     Assessors can view assessments they've entered
        ///     Managers can view all assessments
        /// </summary>
        /// <param name="studentId"></param>
        /// <param name="enteredBy"></param>
        /// <returns></returns>
        public bool CheckStudentAssessmentViewAccess(int? studentId = null, int? enteredBy = null)
        {
            var currentUser = userHelper.GetCurrentUser();
            if (userHelper.HasPermission(rapsContext, currentUser, ManagerPermission) ||
                userHelper.HasPermission(rapsContext, currentUser, AssessmentsViewPermission))
            {
                return true;
            }
            if (userHelper.HasPermission(rapsContext, currentUser, AssessClinicalPermission)
                && enteredBy != null && enteredBy == currentUser?.AaudUserId)
            {
                return true;
            }
            if (studentId == currentUser?.AaudUserId)
            {
                return true;
            }

            return false;
        }

        public bool CanEditStudentAssessment(int enteredBy)
        {
            return userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), ManagerPermission)
                || (userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), AssessClinicalPermission) && enteredBy == userHelper.GetCurrentUser()?.AaudUserId);
        }
    }
}
