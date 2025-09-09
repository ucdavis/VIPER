using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Base class for integration tests that require AAUD and RAPS contexts.
    /// Provides pre-configured contexts and common setup methods to eliminate
    /// repetitive test data setup across integration test files.
    /// </summary>
    public abstract class IntegrationTestBase : ClinicalSchedulerTestBase
    {
        protected readonly AAUDContext AaudContext;
        protected readonly RAPSContext RapsContext;

        protected IntegrationTestBase()
        {
            // Create pre-configured contexts with standard test data
            AaudContext = TestDataBuilder.IntegrationHelpers.CreateAAUDContext();
            RapsContext = TestDataBuilder.IntegrationHelpers.CreateRAPSContext();
        }

        /// <summary>
        /// Sets up the test user with manage permission and all required RAPS data
        /// </summary>
        protected void SetupUserWithManagePermission()
        {
            TestDataBuilder.IntegrationHelpers.AddMemberPermissions(
                RapsContext,
                TestUserMothraId,
                ClinicalSchedulePermissions.Base,
                ClinicalSchedulePermissions.Manage,
                CardiologyEditPermission
            );

            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] {
                ClinicalSchedulePermissions.Base,
                ClinicalSchedulePermissions.Manage,
                CardiologyEditPermission
            });
        }

        /// <summary>
        /// Sets up user permissions for integration tests with real RapsContext instead of MockRapsContext.
        /// This method ensures MockUserHelper works with both real and mock contexts for integration testing.
        /// </summary>
        protected void SetupUserWithPermissionsForIntegration(string? userMothraId, IEnumerable<string> permissions)
        {
            if (userMothraId == null)
            {
                MockUserHelper.Setup(x => x.GetCurrentUser()).Returns((AaudUser?)null);
                return;
            }

            var testUser = CreateTestUser();
            testUser.MothraId = userMothraId;
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            // Default all permissions to false for any context
            MockUserHelper.Setup(x => x.HasPermission(It.IsAny<RAPSContext>(), It.IsAny<AaudUser>(), It.IsAny<string>()))
                .Returns(false);

            // Set up the specific permissions to return true for the real RapsContext
            foreach (var permission in permissions)
            {
                MockUserHelper.Setup(x => x.HasPermission(RapsContext, testUser, permission))
                    .Returns(true);
                // Also set up for the MockRapsContext for backward compatibility
                MockUserHelper.Setup(x => x.HasPermission(MockRapsContext.Object, testUser, permission))
                    .Returns(true);
            }
        }

        /// <summary>
        /// Sets up authenticated HttpContext with required services for controllers
        /// </summary>
        protected void SetupControllerContext(ControllerBase controller)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<RAPSContext>(_ => RapsContext);
            serviceCollection.AddScoped<AAUDContext>(_ => AaudContext);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider;
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, TestUserLoginId) },
                    "test"
                )
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        /// <summary>
        /// Seeds standard clinician test data in both contexts
        /// </summary>
        /// <param name="baseYear">Base year for test data. If not provided, uses current year for convenience.</param>
        protected void SeedCliniciansTestData(int? baseYear = null)
        {
            var currentYear = baseYear ?? DateTime.Now.Year;

            // Add Week and WeekGradYear data
            var (week1, weekGradYear1) = TestDataBuilder.CreateWeekScenario(10, currentYear + 3,
                new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var (week2, weekGradYear2) = TestDataBuilder.CreateWeekScenario(11, currentYear + 3,
                new DateTime(currentYear, 1, 8, 0, 0, 0, DateTimeKind.Utc));

            Context.Weeks.AddRange(week1, week2);
            Context.WeekGradYears.AddRange(weekGradYear1, weekGradYear2);

            // Add instructor schedules with numeric MothraIds
            var instructorSchedules = new[]
            {
                new Models.ClinicalScheduler.InstructorSchedule
                {
                    InstructorScheduleId = 100,
                    MothraId = "12345",
                    RotationId = CardiologyRotationId,
                    WeekId = 10,
                    Evaluator = false
                },
                new Models.ClinicalScheduler.InstructorSchedule
                {
                    InstructorScheduleId = 101,
                    MothraId = "67890",
                    RotationId = SurgeryRotationId,
                    WeekId = 11,
                    Evaluator = true
                }
            };
            Context.InstructorSchedules.AddRange(instructorSchedules);

            // Add clinicians to both contexts
            TestDataBuilder.IntegrationHelpers.SeedCliniciansData(Context, AaudContext);
            Context.SaveChanges();
            AaudContext.SaveChanges();
        }

        /// <summary>
        /// Creates a complete schedule scenario for integration tests
        /// </summary>
        /// <param name="baseYear">Base year for test data. If not provided, uses current year for convenience.</param>
        protected void SeedCompleteScheduleScenario(int? baseYear = null)
        {
            TestDataBuilder.IntegrationHelpers.CreateCompleteScheduleScenario(Context, baseYear);
            Context.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AaudContext?.Dispose();
                RapsContext?.Dispose();
            }
            base.Dispose(disposing);
            GC.SuppressFinalize(this);
        }
    }
}
