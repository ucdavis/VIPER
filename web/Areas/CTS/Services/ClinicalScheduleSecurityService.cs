
using Viper.Classes.SQLContext;

namespace Viper.Areas.CTS.Services
{
	/// <summary>
	/// Clinical schedule permission checking. Should probably be moved into clinical scheduler when that app is converted.
	/// </summary>
	public class ClinicalScheduleSecurityService
	{
		private static readonly string MANAGE_PERM = "SVMSecure.ClnSched.Manage";
		private static readonly string VIEWSTD_PERM = "SVMSecure.ClnSched.ViewStdSchedules";
		private static readonly string VIEWOWN_PERM = "SVMSecure.ClnSched.MySchedule";

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
			if(user != null)
			{
				if(uh.HasPermission(rapsContext, user, MANAGE_PERM) || uh.HasPermission(rapsContext, user, VIEWSTD_PERM))
				{
					return true;
				}
				if(mothraId != null && mothraId == user.MothraId && uh.HasPermission(rapsContext, user, VIEWOWN_PERM))
				{
					return true;
				}
				//NB: some access rules not implemented
			}
			return false;
		}
	}
}
