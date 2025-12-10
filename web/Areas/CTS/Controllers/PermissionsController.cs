using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CTS.Models;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/permissions/")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class PermissionsController : ApiController
    {
        private readonly CtsSecurityService ctsSecurityService;
        private readonly RAPSContext _rapsContext;
        private readonly VIPERContext _viperContext;

        public PermissionsController(VIPERContext context, RAPSContext rapsContext)
        {
            _viperContext = context;
            _rapsContext = rapsContext;
            ctsSecurityService = new CtsSecurityService(rapsContext, _viperContext);
        }

        [HttpGet]
        public ActionResult<PermissionDto> HasAccess(string access, int studentId)
        {
            switch(access)
            {
                case "ViewStudentAssessments":
                    return new PermissionDto {
						HasAccess = ctsSecurityService.CheckStudentAssessmentViewAccess(studentId)
					};
                case "ViewAllAssessments":
					return new PermissionDto {
                    	HasAccess = ctsSecurityService.CheckStudentAssessmentViewAccess()
					};
            }
            return new PermissionDto
			{
				HasAccess = false
			};
        }
    }
}
