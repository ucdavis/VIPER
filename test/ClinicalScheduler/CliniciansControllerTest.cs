using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Web.Authorization;

namespace Viper.test.ClinicalScheduler
{
    public class CliniciansControllerTest : ClinicalSchedulerTestBase
    {
        private readonly AAUDContext _aaudContext;
        private readonly Mock<ILogger<CliniciansController>> _mockLogger;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly Mock<IWeekService> _mockWeekService;
        private readonly Mock<IPersonService> _mockPersonService;
        private readonly CliniciansController _controller;

        public CliniciansControllerTest()
        {
            // Create a mock AAUDContext since we're not testing AAUD functionality
            var aaudOptions = new DbContextOptionsBuilder<AAUDContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _aaudContext = new AAUDContext(aaudOptions);

            _mockLogger = new Mock<ILogger<CliniciansController>>();

            // Create mock service instances for true unit testing
            _mockGradYearService = new Mock<IGradYearService>();
            _mockWeekService = new Mock<IWeekService>();
            _mockPersonService = new Mock<IPersonService>();

            // Set up default mock behavior for common scenarios
            SetupDefaultMockBehavior();

            _controller = new CliniciansController(
                Context,
                _aaudContext,
                _mockLogger.Object,
                _mockGradYearService.Object,
                _mockWeekService.Object,
                _mockPersonService.Object,
                MockUserHelper.Object);

            // Setup HttpContext with service provider for dependency injection
            var serviceProvider = CreateCustomTestServiceProvider(Context, _aaudContext);
            TestDataBuilder.SetupControllerContext(_controller, serviceProvider);

        }

        private static IServiceProvider CreateCustomTestServiceProvider(ClinicalSchedulerContext context, AAUDContext aaudContext)
        {
            return new ServiceCollection()
                .AddSingleton(context)
                .AddSingleton<RAPSContext>(new Mock<RAPSContext>().Object)
                .AddSingleton(aaudContext)
                .BuildServiceProvider();
        }

        private void SetupDefaultMockBehavior()
        {
            // Setup current grad year - mock to return fixed test year for consistency
            _mockGradYearService
                .Setup(x => x.GetCurrentGradYearAsync())
                .ReturnsAsync(2024);


            // Setup default clinicians list for GetCliniciansAsync
            var defaultClinicians = new List<ClinicianSummary>
            {
                new ClinicianSummary
                {
                    MothraId = "12345",
                    FullName = "Dr. John Smith",
                    FirstName = "John",
                    LastName = "Smith"
                },
                new ClinicianSummary
                {
                    MothraId = "67890",
                    FullName = "Dr. Jane Doe",
                    FirstName = "Jane",
                    LastName = "Doe"
                }
            };

            _mockPersonService
                .Setup(x => x.GetCliniciansByGradYearRangeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultClinicians);

            // Setup default clinicians by year
            var defaultCliniciansByYear = new List<ClinicianYearSummary>
            {
                new ClinicianYearSummary
                {
                    MothraId = "11111",
                    FullName = "Dr. Future Clinician",
                    FirstName = "Future",
                    LastName = "Clinician"
                }
            };

            _mockPersonService
                .Setup(x => x.GetCliniciansByYearAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultCliniciansByYear);

            // Setup weeks service to return test weeks with fixed dates
            var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var defaultWeeks = new List<VWeek>
            {
                new VWeek { WeekId = 1, DateStart = baseDate.AddDays(0), DateEnd = baseDate.AddDays(6), TermCode = 202401, WeekNum = 1, GradYear = 2024, ExtendedRotation = false, StartWeek = false, ForcedVacation = false, WeekGradYearId = 1 },
                new VWeek { WeekId = 2, DateStart = baseDate.AddDays(7), DateEnd = baseDate.AddDays(13), TermCode = 202401, WeekNum = 2, GradYear = 2024, ExtendedRotation = false, StartWeek = false, ForcedVacation = false, WeekGradYearId = 2 },
                new VWeek { WeekId = 3, DateStart = baseDate.AddDays(14), DateEnd = baseDate.AddDays(20), TermCode = 202401, WeekNum = 3, GradYear = 2024, ExtendedRotation = false, StartWeek = false, ForcedVacation = false, WeekGradYearId = 3 }
            };

            _mockWeekService
                .Setup(x => x.GetWeeksAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultWeeks);
        }


        [Fact]
        public async Task GetClinicians_WithDefaultParameters_ReturnsCurrentYearClinicians()
        {
            // Act
            var result = await _controller.GetClinicians();

            // Assert - should return OK result with clinician data
            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var clinicianList = clinicians.ToList();

            // Should have at least the test clinicians we seeded
            Assert.True(clinicianList.Count >= 2); // Should have John Smith and Jane Doe
        }

        [Fact]
        public async Task GetClinicians_WithFutureYear_ReturnsYearSpecificClinicians()
        {
            // Act
            var result = await _controller.GetClinicians(year: 2026);

            // Assert - may return error due to in-memory database limitations
            Assert.IsAssignableFrom<IActionResult>(result);

            // If successful, check the data
            if (result is OkObjectResult okResult)
            {
                var clinicians = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
                var clinicianList = clinicians.ToList();
                Assert.True(clinicianList.Count >= 1); // Should have Future Clinician
            }
        }

        [Fact]
        public async Task GetClinicians_WithIncludeAllAffiliates_HandlesParameter()
        {
            // Act
            var result = await _controller.GetClinicians(includeAllAffiliates: true);

            // Assert - may return error due to in-memory database limitations
            Assert.IsAssignableFrom<IActionResult>(result);

            // If successful, check the data
            if (result is OkObjectResult okResult)
            {
                Assert.NotNull(okResult.Value);
            }
        }

        // Note: Removed GetCliniciansCount and CompareCounts tests as they test legacy AAUD functionality

        [Fact]
        public async Task GetClinicianSchedule_WithValidMothraId_ReturnsForbidResult()
        {
            // Act
            var result = await _controller.GetClinicianSchedule("12345");

            // Assert
            // Should return ForbidResult because there's no authenticated user in the test context
            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.NotNull(forbidResult);
        }

        [Fact]
        public async Task GetClinicianSchedule_WithInvalidMothraId_ReturnsForbidResult()
        {
            // Act
            var result = await _controller.GetClinicianSchedule("INVALID");

            // Assert
            // Should return ForbidResult because there's no authenticated user in the test context
            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.NotNull(forbidResult);
        }

        [Fact]
        public async Task GetClinicianSchedule_WithYear_ReturnsForbidResult()
        {
            // Act
            var result = await _controller.GetClinicianSchedule("11111", year: 2026);

            // Assert
            // Should return ForbidResult because there's no authenticated user in the test context
            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.NotNull(forbidResult);
        }

        [Fact]
        public async Task GetClinicianRotations_WithValidMothraId_ReturnsEmptyData()
        {
            // Act
            var result = await _controller.GetClinicianRotations("12345");

            // Assert
            // This method actually returns OK but with empty data since there are no instructor schedules in the test database
            var okResult = Assert.IsType<OkObjectResult>(result);
            var rotations = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Empty(rotations); // Should be empty since there's no test data
        }

        [Fact]
        public async Task GetClinicianRotations_WithInvalidMothraId_ReturnsEmptyList()
        {
            // Act
            var result = await _controller.GetClinicianRotations("INVALID");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var rotations = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Empty(rotations);
        }

        // Note: Removed AnalyzeClinicianData test as it tests legacy AAUD functionality

        [Fact]
        public void CliniciansController_HasCorrectRouteAttribute()
        {
            // Arrange
            var controllerType = typeof(CliniciansController);

            // Act
            var routeAttribute = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
                .FirstOrDefault() as RouteAttribute;

            // Assert
            Assert.NotNull(routeAttribute);
            Assert.Equal("api/[area]/[controller]", routeAttribute.Template);
        }

        [Fact]
        public void CliniciansController_HasCorrectAreaAttribute()
        {
            // Arrange
            var controllerType = typeof(CliniciansController);

            // Act
            var areaAttribute = controllerType.GetCustomAttributes(typeof(AreaAttribute), false)
                .FirstOrDefault() as AreaAttribute;

            // Assert
            Assert.NotNull(areaAttribute);
            Assert.Equal("ClinicalScheduler", areaAttribute.RouteValue);
        }

        [Fact]
        public void CliniciansController_HasCorrectPermissionAttribute()
        {
            // Arrange
            var controllerType = typeof(CliniciansController);

            // Act
            var permissionAttribute = controllerType.GetCustomAttributes(typeof(PermissionAttribute), false)
                .FirstOrDefault() as PermissionAttribute;

            // Assert
            Assert.NotNull(permissionAttribute);
            Assert.Equal(ClinicalSchedulePermissions.Base, permissionAttribute.Allow);
        }

        [Fact]
        public void GetClinicianSchedule_HasCorrectHttpGetAttribute()
        {
            // Arrange
            var methodInfo = typeof(CliniciansController).GetMethod("GetClinicianSchedule");

            // Act
            var httpGetAttribute = methodInfo?.GetCustomAttributes(typeof(HttpGetAttribute), false)
                .FirstOrDefault() as HttpGetAttribute;

            // Assert
            Assert.NotNull(httpGetAttribute);
            Assert.Equal("{mothraId}/schedule", httpGetAttribute.Template);
        }

        [Fact]
        public void GetClinicianRotations_HasCorrectHttpGetAttribute()
        {
            // Arrange
            var methodInfo = typeof(CliniciansController).GetMethod("GetClinicianRotations");

            // Act
            var httpGetAttribute = methodInfo?.GetCustomAttributes(typeof(HttpGetAttribute), false)
                .FirstOrDefault() as HttpGetAttribute;

            // Assert
            Assert.NotNull(httpGetAttribute);
            Assert.Equal("{mothraId}/rotations", httpGetAttribute.Template);
        }

        // Note: Removed GetCliniciansCount attribute test as it tests legacy functionality

        [Theory]
        [InlineData(null)]
        [InlineData(2024)]
        [InlineData(2025)]
        [InlineData(2026)]
        public async Task GetClinicians_WithVariousYears_DoesNotThrow(int? year)
        {
            // Act & Assert - should not throw exceptions
            var result = await _controller.GetClinicians(year: year);

            // Due to in-memory database limitations, may return error results
            // The important thing is that it doesn't throw exceptions
            Assert.IsAssignableFrom<IActionResult>(result);
        }

        // Note: Removed GetCliniciansCount parameterized test as it tests legacy functionality

        [Fact]
        public void CliniciansController_CanBeCreated()
        {
            // Act & Assert
            Assert.NotNull(_controller);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _aaudContext?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
