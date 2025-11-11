using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Research.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.Research.Controllers
{
	[Route("/api/research/aggieEnterpriseReports")]
	[Permission(Allow = "SVMSecure.Research")]
	public class AggieEnterpriseReportsController : ApiController
	{
		private readonly VIPERContext context;
        private readonly AggieEnterpriseReports aggieEnterpriseReports;
        private static readonly string pgmFileName = "AggieEnterprise_PGMData.xlsx";
        private static readonly string glFileName = "AggieEnterprise_GLSummary.xlsx";
        private static readonly string outputFileName = "AggieEnterprise_ReportOutput.xlsx";


        public AggieEnterpriseReportsController(VIPERContext context)
		{
			this.context = context;
			this.aggieEnterpriseReports = new AggieEnterpriseReports(context);
		}

        [HttpPost("upload")]
        public async Task<ActionResult> UploadData(IFormFile pgmData, IFormFile glSummary)
        {
            ClearFiles();
            
            if (pgmData.Length == 0)
            {
                return BadRequest("PGM Data is required.");
            }
            if (glSummary.Length == 0)
            {
                return BadRequest("GL Summary is required.");
            }

            var tempPath = Path.GetTempPath();
            var filePath = Path.Combine(tempPath, pgmFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pgmData.CopyToAsync(stream);
            }
            filePath = Path.Combine(tempPath, glFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await glSummary.CopyToAsync(stream);
            }
            return Ok();
        }

        [HttpGet("generateReport")]
        public async Task<ActionResult> GenerateReport([FromQuery] DateOnly fiscalYearStart, [FromQuery] DateOnly fiscalYearEnd)
        {
            if(fiscalYearEnd > fiscalYearStart)
            {
                var tmp = fiscalYearEnd;
                fiscalYearEnd = fiscalYearStart;
                fiscalYearStart = tmp;
            }

            var tempPath = Path.GetTempPath();
            var pgmFilePath = Path.Combine(tempPath, pgmFileName);
            var glFilePath = Path.Combine(tempPath, glFileName);
            if (!System.IO.File.Exists(pgmFilePath) || !System.IO.File.Exists(glFilePath))
            {
                return BadRequest("Required files are missing. Please upload both PGM Data and GL Summary files.");
            }
            var outputFilePath = Path.Combine(tempPath, outputFileName);
            try
            {
                aggieEnterpriseReports.GenerateAD419Report(pgmFilePath, glFilePath, outputFilePath, fiscalYearStart, fiscalYearEnd);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", System.Web.HttpUtility.UrlEncode(outputFileName));
            }
            catch(Exception e)
            {
                return Problem(e.Message);
            }
            
        }

        [HttpGet("filecheck")]
        public ActionResult FileCheck()
        {
            var tempPath = Path.GetTempPath();
            var pgmFilePath = Path.Combine(tempPath, pgmFileName);
            var glFilePath = Path.Combine(tempPath, glFileName);
            string[] files = ["", ""];

            if (System.IO.File.Exists(pgmFilePath))
            {
                files[0] = pgmFileName + " " + System.IO.File.GetLastWriteTime(pgmFilePath).ToString("MM/dd/yyyy");
            }
            if (System.IO.File.Exists(glFilePath))
            {
                files[1] = glFileName + " " + System.IO.File.GetLastWriteTime(glFilePath).ToString("MM/dd/yyyy");
            }

            return Ok(files);
        }

        private static void ClearFiles()
        {
            var tempPath = Path.GetTempPath();
            //check if files exist and clear if so
            if (System.IO.File.Exists(Path.Combine(tempPath, pgmFileName)))
            {
                System.IO.File.Delete(Path.Combine(tempPath, pgmFileName));
            }
            if (System.IO.File.Exists(Path.Combine(tempPath, glFileName)))
            {
                System.IO.File.Delete(Path.Combine(tempPath, glFileName));
            }
        }
    }
}
