using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Permission constants for Clinical Scheduler area
    /// </summary>
    public static class ClinicalSchedulePermissions
    {
        public const string Base = "SVMSecure.ClnSched";
        public const string Manage = "SVMSecure.ClnSched.Manage";
        public const string ViewStudents = "SVMSecure.ClnSched.ViewStdSchedules";
        public const string ViewClinicians = "SVMSecure.ClnSched.ViewClnSchedules";
        public const string ViewOwn = "SVMSecure.ClnSched.MySchedule";
    }

    /// <summary>
    /// Clinical schedule permission checking for Clinical Scheduler area
    /// </summary>
    public class ClinicalScheduleSecurityService : IClinicalScheduleSecurityService
    {
        public static readonly string BASE_PERM = ClinicalSchedulePermissions.Base;
        public static readonly string MANAGE_PERM = ClinicalSchedulePermissions.Manage;
        public static readonly string VIEWSTD_PERM = ClinicalSchedulePermissions.ViewStudents;
        public static readonly string VIEWOWN_PERM = ClinicalSchedulePermissions.ViewOwn;
        public static readonly string VIEWCLN_PERM = ClinicalSchedulePermissions.ViewClinicians;

        private readonly RAPSContext rapsContext;
        public ClinicalScheduleSecurityService(RAPSContext rapsContext)
        {
            this.rapsContext = rapsContext;
        }

        /// <summary>
        /// Check access to student schedules with the given params
        /// - Students can view their own schedule (if they have the MySchedule Permission)
        /// - Students can also view the schedule for a rotation/week they are on (NOT IMPLEMENTED for CTS)
        /// - Accommodation users can view marked students (NOT IMPLEMENTED for CTS)
        /// - Otherwise, Managers and ViewStdSchedules can view all schedules
        /// </summary>
        /// <param name="classYear"></param>
        /// <param name="mothraId"></param>
        /// <param name="rotationId"></param>
        /// <param name="serviceId"></param>
        /// <param name="weekId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public bool CheckStudentScheduleParams(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate)
        {
            var uh = new UserHelper();
            var user = uh.GetCurrentUser();
            if (user != null)
            {
                if (uh.HasPermission(rapsContext, user, MANAGE_PERM) || uh.HasPermission(rapsContext, user, VIEWSTD_PERM))
                {
                    return true;
                }
                if (mothraId != null && mothraId == user.MothraId && uh.HasPermission(rapsContext, user, VIEWOWN_PERM))
                {
                    return true;
                }
                //NB: some access rules not implemented
            }
            return false;
        }


        /// <summary>
        /// Check access to instructor schedule(s) with the given params
        ///	 - Manage access grants access to all schedules
        ///	 - Those with ViewClnSchedules can view all instructor schedules
        ///	 - Instructors can always view their own schedule
        /// </summary>
        /// <param name="mothraId"></param>
        /// <param name="rotationId"></param>
        /// <param name="serviceId"></param>
        /// <param name="weekId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public bool CheckInstructorScheduleParams(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate)
        {
            var uh = new UserHelper();
            var user = uh.GetCurrentUser();
            if (user != null)
            {
                if (uh.HasPermission(rapsContext, user, MANAGE_PERM) || uh.HasPermission(rapsContext, user, VIEWCLN_PERM))
                {
                    return true;
                }
                if (mothraId != null && mothraId == user.MothraId && uh.HasPermission(rapsContext, user, VIEWOWN_PERM))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
