using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CTS.Models;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
	[Route("/cts/courses/")]
	[Permission(Allow = "SVMSecure")]
	public class CourseController : ApiController
	{
		private readonly CrestCourseService courseService;

		public CourseController(VIPERContext context)
		{
			courseService = new(context);
		}

		[HttpGet]
		public async Task<ActionResult<List<Course>>> GetCourses(string? termCode, string? subjectCode, string? courseNum)
		{
			return await courseService.GetCourses(termCode: termCode, subjectCode: subjectCode, courseNum: courseNum);
		}

		[HttpGet]
		[Route("/cts/courses/{courseId}")]
		public async Task<ActionResult<Course>> GetCourse(int courseId)
		{
			var c = await courseService.GetCourse(courseId);
			if(c == null)
			{
				return NotFound();
			}
			return c;
		}
	}
}
