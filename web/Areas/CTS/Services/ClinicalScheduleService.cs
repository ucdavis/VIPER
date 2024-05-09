using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Services
{
	public class ClinicalScheduleService
	{
		private readonly VIPERContext context;
		private readonly RAPSContext rapsContext;
		public ClinicalScheduleService(VIPERContext context, RAPSContext rapsContext)
		{
			this.context = context;
			this.rapsContext = rapsContext;	
		}

		public async Task<List<StudentSchedule>> GetStudentSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate,
			DateTime? endDate)
		{

			return new List<StudentSchedule>();
		}
	}
}
