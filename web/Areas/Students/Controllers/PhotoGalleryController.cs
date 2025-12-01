using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;
using Viper.Areas.Curriculum.Services;
using Viper.Classes;
using Viper.Classes.Utilities;
using Web.Authorization;
using System.Text.Json;

namespace Viper.Areas.Students.Controllers
{
    [Route("/api/students/photos")]
    [Permission(Allow = "SVMSecure.Students.PhotoGallery")]
    [Permission(Allow = "SVMSecure.Students.StudentGroup")]
    public class PhotoGalleryController : ApiController
    {
        private const int PhotoCacheMaxAgeSeconds = 86400 * 120; // ~4 months (academic semester)

        private readonly IPhotoService _photoService;
        private readonly IStudentGroupService _studentGroupService;
        private readonly IPhotoExportService _photoExportService;
        private readonly ICourseService _courseService;
        private readonly TermCodeService _termCodeService;
        private readonly ILogger<PhotoGalleryController> _logger;

        private static readonly HashSet<string> ValidClassLevels = new() { "V1", "V2", "V3", "V4" };
        private static readonly HashSet<string> ValidGroupTypes = new() { "eighths", "twentieths", "teams", "v3specialty" };

        // Use System.Text.Json for Photo Gallery serialization
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        public PhotoGalleryController(
            IPhotoService photoService,
            IStudentGroupService studentGroupService,
            IPhotoExportService photoExportService,
            ICourseService courseService,
            TermCodeService termCodeService,
            ILogger<PhotoGalleryController> logger)
        {
            _photoService = photoService;
            _studentGroupService = studentGroupService;
            _photoExportService = photoExportService;
            _courseService = courseService;
            _termCodeService = termCodeService;
            _logger = logger;
        }

        [HttpGet("gallery/class/{classLevel}")]
        public async Task<IActionResult> GetClassGallery(string classLevel, [FromQuery] bool includeRossStudents = false)
        {
            if (!ValidClassLevels.Contains(classLevel))
            {
                return BadRequest($"Invalid class level. Must be one of: {string.Join(", ", ValidClassLevels)}");
            }

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
                        Format = string.Empty,
                        IncludeRossStudents = includeRossStudents,
                        Title = string.Empty,
                        Subtitle = string.Empty
                    }
                };

                return WrapJsonResponse(viewModel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting class gallery for {ClassLevel}", LogSanitizer.SanitizeString(classLevel));
                return StatusCode(500, "An error occurred while retrieving the photo gallery");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON serialization error for class gallery {ClassLevel}", LogSanitizer.SanitizeString(classLevel));
                return StatusCode(500, "An error occurred while processing the photo gallery");
            }
        }

        [HttpGet("gallery/group/{groupType}/{groupId}")]
        public async Task<IActionResult> GetGroupGallery(string groupType, string groupId, [FromQuery] string? classLevel = null)
        {
            if (groupType == null || !ValidGroupTypes.Contains(groupType.ToLower()))
            {
                return BadRequest($"Invalid group type. Must be one of: {string.Join(", ", ValidGroupTypes)}");
            }

            if (string.IsNullOrWhiteSpace(groupId) || groupId.Length > 50)
            {
                return BadRequest("Invalid group ID. Must be non-empty and less than 50 characters");
            }

            if (!string.IsNullOrEmpty(classLevel) && !ValidClassLevels.Contains(classLevel))
            {
                return BadRequest($"Invalid class level. Must be one of: {string.Join(", ", ValidClassLevels)}");
            }

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
                        Format = string.Empty,
                        IncludeGroups = true,
                        Title = string.Empty,
                        Subtitle = string.Empty
                    }
                };

                return WrapJsonResponse(viewModel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting group gallery for {GroupType}/{GroupId}", LogSanitizer.SanitizeString(groupType), LogSanitizer.SanitizeString(groupId));
                return StatusCode(500, "An error occurred while retrieving the photo gallery");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON serialization error for group gallery {GroupType}/{GroupId}", LogSanitizer.SanitizeString(groupType), LogSanitizer.SanitizeString(groupId));
                return StatusCode(500, "An error occurred while processing the photo gallery");
            }
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetAvailableCourses()
        {
            try
            {
                var courses = await _courseService.GetAvailableCoursesForPhotosAsync("VET");
                return Ok(courses);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting available courses");
                return StatusCode(500, "An error occurred while retrieving courses");
            }
        }

        [HttpGet("gallery/course/{termCode}/{crn}")]
        public async Task<IActionResult> GetCourseGallery(string termCode, string crn, [FromQuery] bool includeRossStudents = false, [FromQuery] string? groupType = null, [FromQuery] string? groupId = null)
        {
            var validationError = ValidateCourseParams(termCode, crn, required: true);
            if (validationError != null)
            {
                return validationError;
            }

            try
            {
                var students = await _studentGroupService.GetStudentsByCourseAsync(termCode, crn, includeRossStudents, groupType, groupId);
                var courseInfo = await _courseService.GetCourseInfoAsync(termCode, crn);

                if (courseInfo == null)
                {
                    return NotFound($"Course not found for term {termCode} and CRN {crn}");
                }

                var viewModel = new PhotoGalleryViewModel
                {
                    CourseInfo = courseInfo,
                    Students = students,
                    ExportOptions = new ExportOptions
                    {
                        Format = string.Empty,
                        IncludeRossStudents = includeRossStudents,
                        Title = string.Empty,
                        Subtitle = string.Empty
                    }
                };

                return WrapJsonResponse(viewModel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting course gallery for {TermCode}/{Crn}", LogSanitizer.SanitizeString(termCode), LogSanitizer.SanitizeString(crn));
                return StatusCode(500, "An error occurred while retrieving the course photo gallery");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON serialization error for course gallery {TermCode}/{Crn}", LogSanitizer.SanitizeString(termCode), LogSanitizer.SanitizeString(crn));
                return StatusCode(500, "An error occurred while processing the course photo gallery");
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
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting gallery menu");
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

                // Add cache headers for browser caching
                Response.Headers.CacheControl = $"public, max-age={PhotoCacheMaxAgeSeconds}";
                Response.Headers.Expires = DateTime.UtcNow.AddSeconds(PhotoCacheMaxAgeSeconds).ToString("R");

                // Add ETag for conditional requests
                var etag = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(photoData)
                )[..16];
                var etagHeader = $"\"{etag}\"";
                Response.Headers.ETag = etagHeader;

                if (Request.Headers.IfNoneMatch.Contains(etagHeader))
                {
                    return StatusCode(304); // Not Modified
                }

                return File(photoData, "image/jpeg");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Photo not found for student {MailId}", LogSanitizer.SanitizeId(mailId));
                return NotFound();
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error getting photo for student {MailId}", LogSanitizer.SanitizeId(mailId));
                return StatusCode(500, "An error occurred while retrieving the photo");
            }
        }

        [HttpGet("default")]
        public async Task<IActionResult> GetDefaultPhoto()
        {
            try
            {
                var photoData = await _photoService.GetStudentPhotoAsync(string.Empty);
                if (photoData != null && photoData.Length > 0)
                {
                    return File(photoData, "image/jpeg");
                }

                return NotFound();
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Default photo file not found");
                return StatusCode(500, "An error occurred while retrieving the default photo");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error getting default photo");
                return StatusCode(500, "An error occurred while retrieving the default photo");
            }
        }

        [HttpPost("export/word")]
        public async Task<IActionResult> ExportToWord([FromBody] PhotoExportRequest request)
        {
            return await ExecuteExport(request, _photoExportService.ExportToWordAsync, "Word");
        }

        [HttpPost("export/pdf")]
        public async Task<IActionResult> ExportToPdf([FromBody] PhotoExportRequest request)
        {
            return await ExecuteExport(request, _photoExportService.ExportToPdfAsync, "PDF");
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
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting available groups");
                return StatusCode(500, "An error occurred while retrieving available groups");
            }
        }

        [HttpGet("metadata/students/{classLevel}")]
        public async Task<IActionResult> GetStudentsInClass(string classLevel)
        {
            if (!ValidClassLevels.Contains(classLevel))
            {
                return BadRequest($"Invalid class level. Must be one of: {string.Join(", ", ValidClassLevels)}");
            }

            try
            {
                var students = await _studentGroupService.GetStudentsByClassLevelAsync(classLevel, false);
                return Ok(students);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting students for class {ClassLevel}", LogSanitizer.SanitizeString(classLevel));
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
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation getting active class years");
                return StatusCode(500, "An error occurred while retrieving class years");
            }
        }

        /// <summary>
        /// Executes a photo export operation (Word or PDF) with common validation and error handling.
        /// </summary>
        /// <param name="request">The export request parameters</param>
        /// <param name="exportFunc">The service function to execute (ExportToWordAsync or ExportToPdfAsync)</param>
        /// <param name="formatName">The format name for error messages ("Word" or "PDF")</param>
        /// <returns>File result if successful, or error result</returns>
        private async Task<IActionResult> ExecuteExport(
            PhotoExportRequest request,
            Func<PhotoExportRequest, Task<PhotoExportResult?>> exportFunc,
            string formatName)
        {
            // Validate class level
            if (!string.IsNullOrEmpty(request.ClassLevel) && !ValidClassLevels.Contains(request.ClassLevel))
            {
                return BadRequest($"Invalid class level. Must be one of: {string.Join(", ", ValidClassLevels)}");
            }

            // Validate group type
            if (!string.IsNullOrEmpty(request.GroupType) && !ValidGroupTypes.Contains(request.GroupType.ToLower()))
            {
                return BadRequest($"Invalid group type. Must be one of: {string.Join(", ", ValidGroupTypes)}");
            }

            // Validate group ID
            if (!string.IsNullOrEmpty(request.GroupId) && (string.IsNullOrWhiteSpace(request.GroupId) || request.GroupId.Length > 50))
            {
                return BadRequest("Invalid group ID. Must be non-empty and less than 50 characters");
            }

            // Validate course parameters
            var validationError = ValidateCourseParams(request.TermCode, request.Crn, required: false);
            if (validationError != null)
            {
                return validationError;
            }

            try
            {
                var result = await exportFunc(request);

                if (result == null || result.FileData == null)
                {
                    return BadRequest($"Failed to generate {formatName} document");
                }

                return File(result.FileData, result.ContentType, result.FileName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation exporting to {Format}", formatName);
                return StatusCode(500, $"An error occurred while generating the {formatName} document");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error exporting to {Format}", formatName);
                return StatusCode(500, $"An error occurred while generating the {formatName} document");
            }
        }

        /// <summary>
        /// Wraps a data object in the ApiResponse format expected by the frontend,
        /// using System.Text.Json serialization to properly handle JsonInclude attributes.
        /// </summary>
        /// <param name="data">The data object to serialize and wrap</param>
        /// <returns>ContentResult with JSON content wrapped in ApiResponse format</returns>
        private ContentResult WrapJsonResponse(object data)
        {
            // Serialize using System.Text.Json to handle JsonInclude attributes
            var jsonString = JsonSerializer.Serialize(data, _jsonOptions);

            // Manually wrap in ApiResponse format expected by frontend
            var wrappedJson = $"{{\"statusCode\":200,\"success\":true,\"result\":{jsonString}}}";

            return Content(wrappedJson, "application/json");
        }

        /// <summary>
        /// Validates term code and CRN parameters for course-based operations
        /// </summary>
        /// <param name="termCode">Term code in YYYYMM format</param>
        /// <param name="crn">Course Reference Number</param>
        /// <param name="required">If true, validates that values are non-empty. If false, only validates format when values are provided.</param>
        /// <returns>BadRequestObjectResult if validation fails, null if valid</returns>
        private BadRequestObjectResult? ValidateCourseParams(string? termCode, string? crn, bool required = false)
        {
            if (required)
            {
                if (string.IsNullOrWhiteSpace(termCode) || termCode.Length != 6 || !termCode.All(char.IsDigit))
                {
                    return BadRequest("Invalid term code. Must be 6 digits (YYYYMM format)");
                }

                if (string.IsNullOrWhiteSpace(crn) || crn.Length > 5)
                {
                    return BadRequest("Invalid CRN. Must be non-empty and less than 6 characters");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(termCode) && (termCode.Length != 6 || !termCode.All(char.IsDigit)))
                {
                    return BadRequest("Invalid term code. Must be 6 digits (YYYYMM format)");
                }

                if (!string.IsNullOrEmpty(crn) && crn.Length > 5)
                {
                    return BadRequest("Invalid CRN. Must be non-empty and less than 6 characters");
                }
            }

            return null;
        }

    }
}
