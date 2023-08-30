using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;

namespace Viper.Areas.CMS.Controllers
{
    [Route("/CMS/[action]")]
    public class CMSController : Controller
    {
        private readonly VIPERContext _viperContext;
        private readonly RAPSContext _rapsContext;

        public CMSController(RAPSContext rapsContext, VIPERContext viperContext)
        {
            _rapsContext = rapsContext;
            _viperContext = viperContext;
        }

        [HttpGet]
        public IActionResult Files(string id = "", string fn = "", string oldURL = "", string ids = "", string fileName = "")
        {
            Data.CMS cms = new(_viperContext, _rapsContext);

            if (ids.Length > 0)
            {
                return cms.DownloadZip(this, ids.Split(','), fileName);
            }
            else
            {
                return cms.ProvideFile(this, id, fn, oldURL);
            }

        }
    }
}
