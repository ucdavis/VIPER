using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Polly;
using Viper.Areas.CMS.Data;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.Students.Controller
{
	[Area("Research")]
	[Route("[area]/[action]")]
	[Authorize(Policy = "2faAuthentication")]
	[Permission(Allow = "SVMSecure.Research")]
	public class ResearchController : AreaController
	{
		private readonly VIPERContext _viperContext;
        public IUserHelper UserHelper;

		public ResearchController(VIPERContext context, IWebHostEnvironment environment)
		{
			_viperContext = context;
			UserHelper = new UserHelper();
		}
	}
}
