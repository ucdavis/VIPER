using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
	public class Course
	{
		public int CourseId { get; set; }
		public string BlockType { get; set; } = null!;
		public string AcademicYear { get; set; } = null!;
		public string? Crn { get; set; }
		public string? SsaCourseNum { get; set; }
		public string? SeqNumb { get; set; }
		public int? CanvasCourseId { get; set; }

		public List<Session> Sessions { get; set; } = new();

		public Course() { }
		public Course(CourseSessionOffering cso)
		{
			CourseId = cso.CourseId;
			BlockType = cso.BlockType;
			AcademicYear = cso.AcademicYear;
			Crn = cso.Crn;
			SsaCourseNum = cso.SsaCourseNum;
			SeqNumb = cso.SeqNumb;
			CanvasCourseId = cso.CanvasCourseId;
		}
	}
}
