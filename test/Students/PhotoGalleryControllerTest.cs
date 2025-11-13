using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Controller;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;

namespace Viper.test.Students
{
    public class PhotoGalleryControllerTest
    {
        private readonly Mock<IPhotoService> _mockPhotoService;
        private readonly Mock<IStudentGroupService> _mockStudentGroupService;
        private readonly Mock<IPhotoExportService> _mockPhotoExportService;
        private readonly Mock<ICourseService> _mockCourseService;
        private readonly Mock<TermCodeService> _mockTermCodeService;
        private readonly Mock<ILogger<PhotoGalleryController>> _mockLogger;
        private readonly PhotoGalleryController _controller;

        public PhotoGalleryControllerTest()
        {
            _mockPhotoService = new Mock<IPhotoService>();
            _mockStudentGroupService = new Mock<IStudentGroupService>();
            _mockPhotoExportService = new Mock<IPhotoExportService>();
            _mockCourseService = new Mock<ICourseService>();
            // TermCodeService requires VIPERContext and CoursesContext, mock with null using MockBehavior.Loose
            _mockTermCodeService = new Mock<TermCodeService>(MockBehavior.Loose, [null!, null!]);
            _mockLogger = new Mock<ILogger<PhotoGalleryController>>();

            _controller = new PhotoGalleryController(
                _mockPhotoService.Object,
                _mockStudentGroupService.Object,
                _mockPhotoExportService.Object,
                _mockCourseService.Object,
                _mockTermCodeService.Object,
                _mockLogger.Object
            );
        }

        #region Input Validation Tests

        [Theory]
        [InlineData("V5")]
        [InlineData("V0")]
        [InlineData("Invalid")]
        [InlineData("")]
        public async Task GetClassGallery_InvalidClassLevel_ReturnsBadRequest(string classLevel)
        {
            // Act
            var result = await _controller.GetClassGallery(classLevel, false);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid class level", badRequestResult.Value.ToString());
        }

        [Theory]
        [InlineData("V1")]
        [InlineData("V2")]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task GetClassGallery_ValidClassLevel_ReturnsContent(string classLevel)
        {
            // Arrange
            _mockStudentGroupService.Setup(s => s.GetStudentsByClassLevelAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<StudentPhoto>());

            // Act
            var result = await _controller.GetClassGallery(classLevel, false);

            // Assert
            Assert.IsType<ContentResult>(result);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("")]
        [InlineData("groups")]
        public async Task GetGroupGallery_InvalidGroupType_ReturnsBadRequest(string groupType)
        {
            // Act
            var result = await _controller.GetGroupGallery(groupType, "1A1", null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid group type", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task GetGroupGallery_GroupIdTooLong_ReturnsBadRequest()
        {
            // Arrange
            var longGroupId = new string('A', 51);

            // Act
            var result = await _controller.GetGroupGallery("eighths", longGroupId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid group ID", badRequestResult.Value.ToString());
        }

        [Theory]
        [InlineData("12345")]  // 5 digits
        [InlineData("20240")]  // Not 6 digits
        [InlineData("ABCDEF")] // Not numeric
        [InlineData("")]       // Empty
        public async Task GetCourseGallery_InvalidTermCode_ReturnsBadRequest(string termCode)
        {
            // Act
            var result = await _controller.GetCourseGallery(termCode, "12345", false);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid term code", badRequestResult.Value.ToString());
        }

        [Theory]
        [InlineData("")]       // Empty
        [InlineData("123456")] // Too long
        public async Task GetCourseGallery_InvalidCrn_ReturnsBadRequest(string crn)
        {
            // Act
            var result = await _controller.GetCourseGallery("202409", crn, false);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid CRN", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task GetCourseGallery_ValidTermCodeAndCrn_CallsServices()
        {
            // Arrange
            _mockStudentGroupService.Setup(s => s.GetStudentsByCourseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<StudentPhoto>());
            _mockCourseService.Setup(c => c.GetCourseInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new CourseInfo
                {
                    TermCode = "202409",
                    Crn = "12345",
                    Title = "Test Course"
                });

            // Act
            var result = await _controller.GetCourseGallery("202409", "12345", false);

            // Assert
            Assert.IsType<ContentResult>(result);
            _mockStudentGroupService.Verify(s => s.GetStudentsByCourseAsync("202409", "12345", false), Times.Once);
            _mockCourseService.Verify(c => c.GetCourseInfoAsync("202409", "12345"), Times.Once);
        }

        [Fact]
        public async Task GetCourseGallery_CourseNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockStudentGroupService.Setup(s => s.GetStudentsByCourseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<StudentPhoto>());
            _mockCourseService.Setup(c => c.GetCourseInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((CourseInfo)null);

            // Act
            var result = await _controller.GetCourseGallery("202409", "12345", false);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Course not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task GetExportStatus_InvalidGuid_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetExportStatus("not-a-guid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid export ID", badRequestResult.Value.ToString());
        }

        #endregion

        #region Export Tests

        [Fact]
        public async Task ExportToWord_ValidRequest_ReturnsFile()
        {
            // Arrange
            var request = new PhotoExportRequest
            {
                ClassLevel = "V4",
                IncludeRossStudents = false
            };

            _mockPhotoExportService.Setup(e => e.ExportToWordAsync(It.IsAny<PhotoExportRequest>()))
                .ReturnsAsync(new PhotoExportResult
                {
                    ExportId = Guid.NewGuid().ToString(),
                    FileData = new byte[] { 0x50, 0x4B }, // ZIP header for DOCX
                    FileName = "test.docx",
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                });

            // Act
            var result = await _controller.ExportToWord(request);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileResult.ContentType);
        }

        [Fact]
        public async Task ExportToPdf_ValidRequest_ReturnsFile()
        {
            // Arrange
            var request = new PhotoExportRequest
            {
                ClassLevel = "V3",
                IncludeRossStudents = false
            };

            _mockPhotoExportService.Setup(e => e.ExportToPdfAsync(It.IsAny<PhotoExportRequest>()))
                .ReturnsAsync(new PhotoExportResult
                {
                    ExportId = Guid.NewGuid().ToString(),
                    FileData = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // PDF header
                    FileName = "test.pdf",
                    ContentType = "application/pdf"
                });

            // Act
            var result = await _controller.ExportToPdf(request);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
        }

        [Fact]
        public async Task ExportToWord_InvalidClassLevel_ReturnsBadRequest()
        {
            // Arrange
            var request = new PhotoExportRequest
            {
                ClassLevel = "V5",
                IncludeRossStudents = false
            };

            // Act
            var result = await _controller.ExportToWord(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid class level", badRequestResult.Value.ToString());
        }

        #endregion

        #region Route Tests

        [Fact]
        public void Controller_HasCorrectRouteAttribute()
        {
            // Arrange
            var controllerType = typeof(PhotoGalleryController);

            // Act
            var routeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), false);

            // Assert
            Assert.NotEmpty(routeAttributes);
            var routeAttr = (Microsoft.AspNetCore.Mvc.RouteAttribute)routeAttributes[0];
            Assert.Equal("/api/students/photos", routeAttr.Template);
        }

        #endregion
    }
}
