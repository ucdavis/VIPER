using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;
using Viper.Services;

namespace Viper.Areas.CMS.Controllers
{
    [Route("/CMS/[action]")]
    public class CMSController : Controller
    {
        private readonly VIPERContext _viperContext;
        private readonly RAPSContext _rapsContext;
        private readonly IHtmlSanitizerService _sanitizerService;
        private readonly ILogger<Data.CMS> _cmsLogger;

        public CMSController(RAPSContext rapsContext, VIPERContext viperContext, IHtmlSanitizerService sanitizerService, ILoggerFactory loggerFactory)
        {
            _rapsContext = rapsContext;
            _viperContext = viperContext;
            _sanitizerService = sanitizerService;
            //the logger belongs to Data.CMS (which does the logging), not this controller
            _cmsLogger = loggerFactory.CreateLogger<Data.CMS>();
        }

        [HttpGet]
        public IActionResult Files(string id = "", string fn = "", string oldURL = "", string ids = "", string fileName = "")
        {
            Data.CMS cms = new(_viperContext, _rapsContext, _sanitizerService, _cmsLogger);

            if (ids.Length > 0)
            {
                return cms.DownloadZip(this, ids.Split(','), fileName);
            }

            return cms.ProvideFile(this, id, fn, oldURL);

        }
    }
}
