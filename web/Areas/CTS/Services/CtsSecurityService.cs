using Viper.Classes.SQLContext;

namespace Viper.Areas.CTS.Services
{
    public class CtsSecurityService
    {
        private readonly RAPSContext rapsContext;
        private readonly VIPERContext viperContext;
        public IUserHelper userHelper;

        public CtsSecurityService(RAPSContext rapsContext, VIPERContext viperContext)
        {
            this.rapsContext = rapsContext;
            this.viperContext = viperContext;
            userHelper = new UserHelper();
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
            if(userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.Manage"))
            {
                return true;
            }
            if (userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), "SVMSecure.CTS.StudentAssessments")
                && enteredBy == userHelper.GetCurrentUser()?.AaudUserId)
            {
                return true;
            }
            if(studentId == userHelper.GetCurrentUser()?.AaudUserId)
            {
                return true;
            }

            return false;
        }
    }
}
