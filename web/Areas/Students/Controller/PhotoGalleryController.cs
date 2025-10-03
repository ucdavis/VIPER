using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;
using Viper.Areas.Curriculum.Services;
using Web.Authorization;

namespace Viper.Areas.Students.Controller
{
    [Area("Students")]
    [Route("/api/students/photos")]
    [ApiController]
    [Authorize(Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure.Students")]
    public class PhotoGalleryController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        private readonly IStudentGroupService _studentGroupService;
        private readonly IPhotoExportService _photoExportService;
        private readonly TermCodeService _termCodeService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<PhotoGalleryController> _logger;

        public PhotoGalleryController(
            IPhotoService photoService,
            IStudentGroupService studentGroupService,
            IPhotoExportService photoExportService,
            TermCodeService termCodeService,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<PhotoGalleryController> logger)
        {
            _photoService = photoService;
            _studentGroupService = studentGroupService;
            _photoExportService = photoExportService;
            _termCodeService = termCodeService;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [HttpGet("gallery/class/{classLevel}")]
        public async Task<IActionResult> GetClassGallery(string classLevel, [FromQuery] bool includeRossStudents = false)
        {
            try
            {
                var students = await _studentGroupService.GetStudentsByClassLevelAsync(classLevel, includeRossStudents);

                var viewModel = new PhotoGalleryViewModel
                {
                    ClassLevel = classLevel,
                    Students = students,
                    GroupInfo = null,
                    ExportOptions = new ExportOptions
                    {
                        IncludeRossStudents = includeRossStudents
                    }
                };

                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class gallery for {ClassLevel}", classLevel);
                return StatusCode(500, "An error occurred while retrieving the photo gallery");
            }
        }

        [HttpGet("gallery/group/{groupType}/{groupId}")]
        public async Task<IActionResult> GetGroupGallery(string groupType, string groupId, [FromQuery] string? classLevel = null)
        {
            try
            {
                var students = await _studentGroupService.GetStudentsByGroupAsync(groupType, groupId, classLevel);
                var groupInfo = await _studentGroupService.GetGroupingInfoAsync(groupType);
                groupInfo.GroupId = groupId;

                var viewModel = new PhotoGalleryViewModel
                {
                    Students = students,
                    GroupInfo = groupInfo,
                    ExportOptions = new ExportOptions
                    {
                        IncludeGroups = true
                    }
                };

                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group gallery for {GroupType}/{GroupId}", groupType, groupId);
                return StatusCode(500, "An error occurred while retrieving the photo gallery");
            }
        }

        [HttpGet("gallery/menu")]
        public async Task<IActionResult> GetGalleryMenu()
        {
            try
            {
                var menu = new
                {
                    ClassLevels = new[] { "V1", "V2", "V3", "V4" },
                    GroupTypes = new[]
                    {
                        new { Type = "eighths", Label = "Eighths", Groups = await _studentGroupService.GetEighthsGroupsAsync() },
                        new { Type = "twentieths", Label = "Twentieths", Groups = await _studentGroupService.GetTwentiethsGroupsAsync() },
                        new { Type = "teams", Label = "Teams (V3)", Groups = await _studentGroupService.GetTeamsAsync("V3") },
                        new { Type = "v3specialty", Label = "V3 Specialty", Groups = await _studentGroupService.GetV3SpecialtyGroupsAsync() }
                    }
                };

                return Ok(menu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gallery menu");
                return StatusCode(500, "An error occurred while retrieving the gallery menu");
            }
        }

        [HttpGet("student/{mailId}")]
        public async Task<IActionResult> GetStudentPhoto(string mailId)
        {
            try
            {
                var photoData = await _photoService.GetStudentPhotoAsync(mailId);

                if (photoData == null || photoData.Length == 0)
                {
                    return NotFound();
                }

                return File(photoData, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting photo for student {MailId}", mailId);
                return StatusCode(500, "An error occurred while retrieving the photo");
            }
        }

        [HttpGet("default")]
        public async Task<IActionResult> GetDefaultPhoto()
        {
            try
            {
                var idCardPhotoPath = _configuration["PhotoGallery:IDCardPhotoPath"];
                var defaultPhotoFile = _configuration["PhotoGallery:DefaultPhotoFile"] ?? "nopic.jpg";

                // Try configured path first (e.g., S:\Files\IDCardPhotos\ in production)
                if (!string.IsNullOrEmpty(idCardPhotoPath))
                {
                    var defaultPhotoPath = Path.Combine(idCardPhotoPath, defaultPhotoFile);
                    if (System.IO.File.Exists(defaultPhotoPath))
                    {
                        var photoData = await System.IO.File.ReadAllBytesAsync(defaultPhotoPath);
                        return File(photoData, "image/jpeg");
                    }
                }

                // Fallback to wwwroot/images/nopic.jpg
                var wwwrootPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", defaultPhotoFile);
                if (System.IO.File.Exists(wwwrootPath))
                {
                    var photoData = await System.IO.File.ReadAllBytesAsync(wwwrootPath);
                    return File(photoData, "image/jpeg");
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default photo");
                return StatusCode(500, "An error occurred while retrieving the default photo");
            }
        }

        [HttpPost("export/word")]
        public async Task<IActionResult> ExportToWord([FromBody] PhotoExportRequest request)
        {
            try
            {
                var result = await _photoExportService.ExportToWordAsync(request);

                if (result == null || result.FileData == null)
                {
                    return BadRequest("Failed to generate Word document");
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Word");
                return StatusCode(500, "An error occurred while generating the Word document");
            }
        }

        [HttpPost("export/pdf")]
        public async Task<IActionResult> ExportToPdf([FromBody] PhotoExportRequest request)
        {
            try
            {
                var result = await _photoExportService.ExportToPdfAsync(request);

                if (result == null || result.FileData == null)
                {
                    return BadRequest("Failed to generate PDF document");
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                return StatusCode(500, "An error occurred while generating the PDF document");
            }
        }

        [HttpGet("export/status/{exportId}")]
        public async Task<IActionResult> GetExportStatus(string exportId)
        {
            try
            {
                var status = await _photoExportService.GetExportStatusAsync(exportId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting export status for {ExportId}", exportId);
                return StatusCode(500, "An error occurred while checking export status");
            }
        }

        [HttpGet("metadata/groups")]
        public async Task<IActionResult> GetAvailableGroups()
        {
            try
            {
                var groups = new
                {
                    Eighths = await _studentGroupService.GetEighthsGroupsAsync(),
                    Twentieths = await _studentGroupService.GetTwentiethsGroupsAsync(),
                    TeamsV3 = await _studentGroupService.GetTeamsAsync("V3")
                };

                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available groups");
                return StatusCode(500, "An error occurred while retrieving available groups");
            }
        }

        [HttpGet("metadata/students/{classLevel}")]
        public async Task<IActionResult> GetStudentsInClass(string classLevel)
        {
            try
            {
                var students = await _studentGroupService.GetStudentsByClassLevelAsync(classLevel, false);
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students for class {ClassLevel}", classLevel);
                return StatusCode(500, "An error occurred while retrieving students");
            }
        }

        [HttpGet("metadata/classyears")]
        public async Task<IActionResult> GetActiveClassYears()
        {
            try
            {
                var classYears = await _termCodeService.GetActiveClassYears();

                // Map years to class levels (V4, V3, V2, V1)
                var classLevelMapping = new List<object>();
                for (int i = 0; i < classYears.Count && i < 4; i++)
                {
                    classLevelMapping.Add(new
                    {
                        Year = classYears[i],
                        ClassLevel = $"V{4 - i}"
                    });
                }

                return Ok(classLevelMapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active class years");
                return StatusCode(500, "An error occurred while retrieving class years");
            }
        }
    }
}
