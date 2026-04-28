using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Controllers;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;

namespace Viper.test.Students
{
    public class PhotoGalleryControllerTest
    {
        private readonly IPhotoService _mockPhotoService;
        private readonly IStudentGroupService _mockStudentGroupService;
        private readonly IPhotoExportService _mockPhotoExportService;
        private readonly ICourseService _mockCourseService;
        private readonly TermCodeService _mockTermCodeService;
        private readonly ILogger<PhotoGalleryController> _mockLogger;
        private readonly PhotoGalleryController _controller;

        public PhotoGalleryControllerTest()
        {
            _mockPhotoService = Substitute.For<IPhotoService>();
            _mockStudentGroupService = Substitute.For<IStudentGroupService>();
            _mockPhotoExportService = Substitute.For<IPhotoExportService>();
            _mockCourseService = Substitute.For<ICourseService>();
            // TermCodeService requires VIPERContext and CoursesContext, mock with null using NSubstitute
            _mockTermCodeService = Substitute.For<TermCodeService>([null!, null!]);
            _mockLogger = Substitute.For<ILogger<PhotoGalleryController>>();

            _controller = new PhotoGalleryController(
                _mockPhotoService,
                _mockStudentGroupService,
                _mockPhotoExportService,
                _mockCourseService,
                _mockTermCodeService,
                _mockLogger
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
            Assert.Contains("Invalid class level", badRequestResult.Value!.ToString()!);
        }

        [Theory]
        [InlineData("V1")]
        [InlineData("V2")]
        [InlineData("V3")]
        [InlineData("V4")]
        public async Task GetClassGallery_ValidClassLevel_ReturnsContent(string classLevel)
        {
            // Arrange
            _mockStudentGroupService.GetStudentsByClassLevelAsync(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(new List<StudentPhoto>());

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
            Assert.Contains("Invalid group type", badRequestResult.Value!.ToString()!);
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
            Assert.Contains("Invalid group ID", badRequestResult.Value!.ToString()!);
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
            Assert.Contains("Invalid term code", badRequestResult.Value!.ToString()!);
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
            Assert.Contains("Invalid CRN", badRequestResult.Value!.ToString()!);
        }

        [Fact]
        public async Task GetCourseGallery_ValidTermCodeAndCrn_CallsServices()
        {
            // Arrange
            _mockStudentGroupService.GetStudentsByCourseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new List<StudentPhoto>());
            _mockCourseService.GetCourseInfoAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new CourseInfo
                {
                    TermCode = "202409",
                    Crn = "12345",
                    Title = "Test Course"
                });

            // Act
            var result = await _controller.GetCourseGallery("202409", "12345", false);

            // Assert
            Assert.IsType<ContentResult>(result);
            await _mockStudentGroupService.Received(1).GetStudentsByCourseAsync("202409", "12345", false, null, null);
            await _mockCourseService.Received(1).GetCourseInfoAsync("202409", "12345");
        }

        [Fact]
        public async Task GetCourseGallery_CourseNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockStudentGroupService.GetStudentsByCourseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new List<StudentPhoto>());
            _mockCourseService.GetCourseInfoAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns((CourseInfo?)null);

            // Act
            var result = await _controller.GetCourseGallery("202409", "12345", false);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Course not found", notFoundResult.Value!.ToString()!);
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

            _mockPhotoExportService.ExportToWordAsync(Arg.Any<PhotoExportRequest>())
                .Returns(new PhotoExportResult
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

            _mockPhotoExportService.ExportToPdfAsync(Arg.Any<PhotoExportRequest>())
                .Returns(new PhotoExportResult
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
            Assert.Contains("Invalid class level", badRequestResult.Value!.ToString()!);
        }

        #endregion

        #region Teams Filter Tests

        [Theory]
        [InlineData("teams")]
        [InlineData("v3specialty")]
        public async Task GetGroupGallery_ValidV3GroupTypes_ReturnsContent(string groupType)
        {
            // Arrange
            _mockStudentGroupService.GetStudentsByGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new List<StudentPhoto>());

            _mockStudentGroupService.GetGroupingInfoAsync(Arg.Any<string>())
                .Returns(new GroupingInfo
                {
                    GroupType = groupType,
                    GroupId = "1",
                    AvailableGroups = new List<string>()
                });

            // Act
            var result = await _controller.GetGroupGallery(groupType, "1", "V3");

            // Assert
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task GetGroupGallery_TeamsGroupType_CallsServiceWithCorrectParameters()
        {
            // Arrange
            _mockStudentGroupService.GetStudentsByGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new List<StudentPhoto>
                {
                    new StudentPhoto
                    {
                        MailId = "test1",
                        FirstName = "John",
                        LastName = "Doe",
                        DisplayName = "John Doe",
                        PhotoUrl = "/api/students/photos/student/test1",
                        TeamNumber = "1",
                        ClassLevel = "V3"
                    }
                });

            _mockStudentGroupService.GetGroupingInfoAsync(Arg.Any<string>())
                .Returns(new GroupingInfo
                {
                    GroupType = "teams",
                    GroupId = "1",
                    AvailableGroups = new List<string> { "1", "2", "3" }
                });

            // Act
            var result = await _controller.GetGroupGallery("teams", "1", "V3");

            // Assert
            await _mockStudentGroupService.Received(1).GetStudentsByGroupAsync("teams", "1", "V3");
            await _mockStudentGroupService.Received(1).GetGroupingInfoAsync("teams");
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task GetCourseGallery_WithTeamsGroupFilter_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var mockStudents = new List<StudentPhoto>
            {
                new StudentPhoto
                {
                    MailId = "test1",
                    FirstName = "John",
                    LastName = "Doe",
                    DisplayName = "John Doe",
                    PhotoUrl = "/api/students/photos/student/test1",
                    TeamNumber = "1",
                    ClassLevel = "V3"
                },
                new StudentPhoto
                {
                    MailId = "test2",
                    FirstName = "Jane",
                    LastName = "Smith",
                    DisplayName = "Jane Smith",
                    PhotoUrl = "/api/students/photos/student/test2",
                    TeamNumber = "1",
                    ClassLevel = "V3"
                }
            };

            _mockStudentGroupService.GetStudentsByCourseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<string>())
                .Returns(mockStudents);

            _mockCourseService.GetCourseInfoAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new CourseInfo
                {
                    TermCode = "202501",
                    Crn = "12345",
                    SubjectCode = "VET",
                    CourseNumber = "400",
                    Title = "V3 Clinical Course"
                });

            // Act
            var result = await _controller.GetCourseGallery("202501", "12345", false, "teams", "1");

            // Assert
            await _mockStudentGroupService.Received(1).GetStudentsByCourseAsync("202501", "12345", false, "teams", "1");
            Assert.IsType<ContentResult>(result);
        }

        [Fact]
        public async Task GetCourseGallery_WithV3Students_ReturnsStudentsWithTeamNumbers()
        {
            // Arrange
            var mockStudents = new List<StudentPhoto>
            {
                new StudentPhoto
                {
                    MailId = "test1",
                    FirstName = "John",
                    LastName = "Doe",
                    DisplayName = "John Doe",
                    PhotoUrl = "/api/students/photos/student/test1",
                    TeamNumber = "1",
                    EighthsGroup = "1A1",
                    TwentiethsGroup = "1AA",
                    ClassLevel = "V3",
                    HasPhoto = true
                }
            };

            _mockStudentGroupService.GetStudentsByCourseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<string>())
                .Returns(mockStudents);

            _mockCourseService.GetCourseInfoAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new CourseInfo
                {
                    TermCode = "202501",
                    Crn = "12345",
                    Title = "Test Course"
                });

            // Act
            var result = await _controller.GetCourseGallery("202501", "12345", false);

            // Assert
            Assert.IsType<ContentResult>(result);
            var contentResult = result as ContentResult;
            Assert.NotNull(contentResult?.Content);

            // Verify service was called
            await _mockStudentGroupService.Received(1).GetStudentsByCourseAsync("202501", "12345", false, null, null);
        }

        [Theory]
        [InlineData("teams", "1")]
        [InlineData("teams", "16")]
        [InlineData("v3specialty", "SA")]
        [InlineData("v3specialty", "LA")]
        public async Task GetCourseGallery_WithV3GroupFilters_AcceptsValidGroupTypes(string groupType, string groupId)
        {
            // Arrange
            _mockStudentGroupService.GetStudentsByCourseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<string>())
                .Returns(new List<StudentPhoto>());

            _mockCourseService.GetCourseInfoAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new CourseInfo
                {
                    TermCode = "202501",
                    Crn = "12345",
                    Title = "Test Course"
                });

            // Act
            var result = await _controller.GetCourseGallery("202501", "12345", false, groupType, groupId);

            // Assert
            Assert.IsType<ContentResult>(result);
            await _mockStudentGroupService.Received(1).GetStudentsByCourseAsync("202501", "12345", false, groupType, groupId);
        }

        #endregion

        #region Route Tests

        [Fact]
        public void Controller_HasCorrectRouteAttribute()
        {
            // Arrange
            var controllerType = typeof(PhotoGalleryController);

            // Act
            var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);

            // Assert
            Assert.NotEmpty(routeAttributes);
            var routeAttr = (RouteAttribute)routeAttributes[0];
            Assert.Equal("/api/students/photos", routeAttr.Template);
        }

        #endregion
    }
}
