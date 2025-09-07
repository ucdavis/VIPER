using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    public class CliniciansControllerTest : ClinicalSchedulerTestBase
    {
        private readonly AAUDContext _aaudContext;
        private readonly Mock<ILogger<CliniciansController>> _mockLogger;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly Mock<IWeekService> _mockWeekService;
        private readonly Mock<IPersonService> _mockPersonService;
        private CliniciansController _controller = null!;

        public CliniciansControllerTest()
        {
            var aaudOptions = new DbContextOptionsBuilder<AAUDContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _aaudContext = new AAUDContext(aaudOptions);

            _mockLogger = new Mock<ILogger<CliniciansController>>();
            _mockGradYearService = new Mock<IGradYearService>();
            _mockWeekService = new Mock<IWeekService>();
            _mockPersonService = new Mock<IPersonService>();

            SetupDefaultMockBehavior();
            RecreateController();
        }

        private void RecreateController()
        {
            _controller = new CliniciansController(
                Context,
                _aaudContext,
                _mockLogger.Object,
                _mockGradYearService.Object,
                _mockWeekService.Object,
                _mockPersonService.Object,
                MockUserHelper.Object);

            var serviceProvider = new ServiceCollection()
                .AddSingleton(Context)
                .AddSingleton(MockRapsContext.Object)
                .AddSingleton(_aaudContext)
                .BuildServiceProvider();
            TestDataBuilder.SetupControllerContext(_controller, serviceProvider);
        }

        private void SetupDefaultMockBehavior()
        {
            _mockGradYearService.Setup(x => x.GetCurrentGradYearAsync()).ReturnsAsync(2024);

            var defaultClinicians = new List<ClinicianSummary>
            {
                new ClinicianSummary { MothraId = TestUserMothraId, FullName = "Test User" },
                new ClinicianSummary { MothraId = "67890", FullName = "Dr. Jane Doe" }
            };
            _mockPersonService.Setup(x => x.GetCliniciansByGradYearRangeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultClinicians);
            var defaultCliniciansByYear = new List<ClinicianYearSummary>
            {
                new ClinicianYearSummary { MothraId = TestUserMothraId, FullName = "Test User" },
                new ClinicianYearSummary { MothraId = "67890", FullName = "Dr. Jane Doe" }
            };
            _mockPersonService.Setup(x => x.GetCliniciansByYearAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultCliniciansByYear);
        }

        [Fact]
        public async Task GetClinicians_ForAdminUser_ReturnsAllClinicians()
        {
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.Admin });
            RecreateController();

            var result = await _controller.GetClinicians();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            Assert.Equal(2, clinicians.Count());
        }

        [Fact]
        public async Task GetClinicians_ForOwnScheduleUser_WithoutViewContext_ReturnsAllClinicians()
        {
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.EditOwnSchedule });
            RecreateController();

            var result = await _controller.GetClinicians();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            // Without viewContext, users with EditOwnSchedule should see all clinicians
            Assert.Equal(2, clinicians.Count());
        }

        [Fact]
        public async Task GetClinicians_ForOwnScheduleUser_InClinicianView_ReturnsOnlyOwnClinician()
        {
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.EditOwnSchedule });
            RecreateController();

            var result = await _controller.GetClinicians(viewContext: "clinician");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            var clinicianList = clinicians.ToList();
            Assert.Single(clinicianList);
            Assert.Equal(TestUserMothraId, clinicianList[0].MothraId);
        }

        [Fact]
        public async Task GetClinicianSchedule_ForAdminUser_ReturnsSchedule()
        {
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.Admin });
            RecreateController();

            var result = await _controller.GetClinicianSchedule("67890"); // Requesting other user's schedule

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task GetClinicianSchedule_ForOwnScheduleUser_ReturnsOwnSchedule()
        {
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.EditOwnSchedule });
            RecreateController();

            var result = await _controller.GetClinicianSchedule(TestUserMothraId);

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task GetClinicianSchedule_ForOwnScheduleUser_ReturnsForbidForOtherSchedule()
        {
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.EditOwnSchedule });
            RecreateController();

            var result = await _controller.GetClinicianSchedule("67890"); // Requesting other user's schedule

            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.Equal("You do not have permission to view this clinician's schedule.", forbidResult.AuthenticationSchemes[0]);
        }

        [Fact]
        public async Task GetClinicianSchedule_WithNoAuthenticatedUser_ReturnsForbid()
        {
            SetupUserWithPermissions(null, new string[] { }); // No user
            RecreateController();

            var result = await _controller.GetClinicianSchedule("12345");

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetClinicians_ForUserWithCombinedPermissions_InClinicianView_ReturnsOnlyOwnClinician()
        {
            // Setup user with both EditOwnSchedule and service-specific permissions
            SetupUserWithPermissions(TestUserMothraId, new[] {
                ClinicalSchedulePermissions.EditOwnSchedule,
                CardiologyEditPermission
            });
            RecreateController();

            var result = await _controller.GetClinicians(viewContext: "clinician");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            var clinicianList = clinicians.ToList();
            Assert.Single(clinicianList);
            Assert.Equal(TestUserMothraId, clinicianList[0].MothraId);
        }

        [Fact]
        public async Task GetClinicians_ForUserWithCombinedPermissions_InRotationView_ReturnsAllClinicians()
        {
            // Setup user with both EditOwnSchedule and service-specific permissions
            SetupUserWithPermissions(TestUserMothraId, new[] {
                ClinicalSchedulePermissions.EditOwnSchedule,
                CardiologyEditPermission
            });
            RecreateController();

            var result = await _controller.GetClinicians(viewContext: "rotation");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            Assert.Equal(2, clinicians.Count()); // Should see all clinicians
        }

        [Fact]
        public async Task GetClinicians_ForUserWithOnlyServicePermissions_InBothViews_ReturnsAllClinicians()
        {
            // Setup user with only service-specific permissions (no EditOwnSchedule)
            SetupUserWithPermissions(TestUserMothraId, new[] { CardiologyEditPermission });
            RecreateController();

            // Test clinician view
            var resultClinician = await _controller.GetClinicians(viewContext: "clinician");
            var okResultClinician = Assert.IsType<OkObjectResult>(resultClinician);
            var cliniciansClinician = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResultClinician.Value);
            Assert.Equal(2, cliniciansClinician.Count()); // Should see all clinicians

            // Test rotation view
            var resultRotation = await _controller.GetClinicians(viewContext: "rotation");
            var okResultRotation = Assert.IsType<OkObjectResult>(resultRotation);
            var cliniciansRotation = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResultRotation.Value);
            Assert.Equal(2, cliniciansRotation.Count()); // Should see all clinicians
        }

        [Fact]
        public async Task GetClinicians_WithNoViewContext_DefaultBehavior_FiltersBasedOnPermissions()
        {
            // Setup user with only EditOwnSchedule permission
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.EditOwnSchedule });
            RecreateController();

            // No viewContext should behave like default (not "clinician")
            var result = await _controller.GetClinicians();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            // Without viewContext="clinician", users with EditOwnSchedule should see all clinicians
            Assert.Equal(2, clinicians.Count());
        }

        [Fact]
        public async Task GetClinicians_ViewContextParameterPassing_WorksCorrectly()
        {
            // Setup user with EditOwnSchedule permission
            SetupUserWithPermissions(TestUserMothraId, new[] { ClinicalSchedulePermissions.EditOwnSchedule });
            RecreateController();

            // Test that viewContext parameter is properly processed
            var resultWithContext = await _controller.GetClinicians(viewContext: "clinician");
            var okResultWithContext = Assert.IsType<OkObjectResult>(resultWithContext);
            var cliniciansWithContext = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResultWithContext.Value);
            Assert.Single(cliniciansWithContext); // Should filter to self in clinician view

            var resultWithoutContext = await _controller.GetClinicians();
            var okResultWithoutContext = Assert.IsType<OkObjectResult>(resultWithoutContext);
            var cliniciansWithoutContext = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResultWithoutContext.Value);
            Assert.Equal(2, cliniciansWithoutContext.Count()); // Should show all without specific context
        }

        [Fact]
        public async Task GetClinicians_ForUserWithMultipleServicePermissions_ReturnsAllClinicians()
        {
            // Setup user with multiple service-specific permissions
            SetupUserWithPermissions(TestUserMothraId, new[] {
                CardiologyEditPermission,
                InternalMedicineEditPermission
            });
            RecreateController();

            var result = await _controller.GetClinicians();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            Assert.Equal(2, clinicians.Count()); // Should see all clinicians regardless of view context
        }

        [Fact]
        public async Task GetClinicians_ForUserWithSingleServicePermission_ReturnsAllClinicians()
        {
            // Setup user with only Cardiology service permission
            SetupUserWithPermissions(TestUserMothraId, new[] { CardiologyEditPermission });
            RecreateController();

            var result = await _controller.GetClinicians(viewContext: "rotation");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clinicians = Assert.IsAssignableFrom<IEnumerable<dynamic>>(okResult.Value);
            Assert.Equal(2, clinicians.Count()); // Should see all clinicians to assign to rotations
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
