using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler.Integration
{
    /// <summary>
    /// Integration tests for the consolidated permission service architecture.
    /// Tests the complete flow of permission checks after consolidating
    /// ClinicalScheduleSecurityService into SchedulePermissionService.
    /// </summary>
    public class PermissionServiceIntegrationTest : IntegrationTestBase
    {
        private readonly Mock<ILogger<SchedulePermissionService>> _mockLogger;
        private readonly SchedulePermissionService _permissionService;

        public PermissionServiceIntegrationTest()
        {
            _mockLogger = new Mock<ILogger<SchedulePermissionService>>();
            _permissionService = new SchedulePermissionService(
                Context,
                RapsContext,
                _mockLogger.Object,
                MockUserHelper.Object
            );
        }

        [Fact]
        public async Task ConsolidatedPermissionService_ReplacesSecurityService_Successfully()
        {
            // Arrange - Test that the consolidated service handles all previous functionality
            var serviceId = CardiologyServiceId;
            SetupUserWithManagePermission();

            // Act - Test methods that were previously in ClinicalScheduleSecurityService
            var hasEditPermission = await _permissionService.HasEditPermissionForServiceAsync(serviceId);
            var requiredPermission = await _permissionService.GetRequiredPermissionForServiceAsync(serviceId);

            // Assert - Verify consolidation maintains backward compatibility
            Assert.True(hasEditPermission);
            Assert.Equal(CardiologyEditPermission, requiredPermission);
        }

        [Fact]
        public async Task DynamicPermissionLookup_FromDatabase_WorksCorrectly()
        {
            // Arrange - Setup service with dynamic permission
            var dynamicPermission = "SVMSecure.ClnSched.Edit.Dynamic";
            var service = TestDataBuilder.CreateService(99, "Dynamic Service", "DYN", dynamicPermission);
            Context.Services.Add(service);
            await Context.SaveChangesAsync();

            // Add permission to RAPS context
            TestDataBuilder.IntegrationHelpers.AddMemberPermissions(RapsContext, TestUserMothraId, dynamicPermission);
            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] { dynamicPermission });

            // Act
            var requiredPermission = await _permissionService.GetRequiredPermissionForServiceAsync(99);
            var hasPermission = await _permissionService.HasEditPermissionForServiceAsync(99);

            // Assert
            Assert.Equal(dynamicPermission, requiredPermission);
            Assert.True(hasPermission);
        }

        [Fact]
        public async Task PermissionCascading_ManageOverridesServiceSpecific()
        {
            // Arrange - User has Manage permission but not service-specific
            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] { ClinicalSchedulePermissions.Manage });

            // Act - Check permission for service with specific permission requirement
            var hasCardiologyPermission = await _permissionService.HasEditPermissionForServiceAsync(CardiologyServiceId);
            var hasSurgeryPermission = await _permissionService.HasEditPermissionForServiceAsync(SurgeryServiceId);

            // Assert - Manage permission should override all service-specific permissions
            Assert.True(hasCardiologyPermission);
            Assert.True(hasSurgeryPermission);
        }

        [Fact]
        public async Task AsyncPermissionCheckMethods_WorkCorrectly()
        {
            // Arrange
            SetupUserWithManagePermission();
            var gradYear = 2024;

            // Add test data for student schedule params using TestDataBuilder
            var (week, weekGradYear) = TestDataBuilder.CreateWeekScenario(1, gradYear,
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Context.Weeks.Add(week);
            Context.WeekGradYears.Add(weekGradYear);
            await Context.SaveChangesAsync();

            // Act - Test new async methods added during consolidation
            var studentScheduleResult = await _permissionService.CheckStudentScheduleParamsAsync(
                mothraId: TestUserMothraId,
                rotationId: null,
                serviceId: null,
                weekId: null,
                startDate: null,
                endDate: null
            );

            var instructorScheduleResult = await _permissionService.CheckInstructorScheduleParamsAsync(
                mothraId: null,
                rotationId: TestInstructorScheduleId,
                serviceId: null,
                weekId: null,
                startDate: null,
                endDate: null
            );

            // Assert
            Assert.True(studentScheduleResult);
            Assert.True(instructorScheduleResult);
        }

        [Fact]
        public async Task ServiceSpecificPermissions_WithoutManage_WorkCorrectly()
        {
            // Arrange - User has only Cardiology permission, not Manage
            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] { CardiologyEditPermission });

            // Act
            var hasCardiologyPermission = await _permissionService.HasEditPermissionForServiceAsync(CardiologyServiceId);
            var hasSurgeryPermission = await _permissionService.HasEditPermissionForServiceAsync(SurgeryServiceId);
            var hasInternalMedicinePermission = await _permissionService.HasEditPermissionForServiceAsync(InternalMedicineServiceId);

            // Assert - Should only have permission for Cardiology
            Assert.True(hasCardiologyPermission);
            Assert.False(hasSurgeryPermission);
            Assert.False(hasInternalMedicinePermission);
        }

        [Fact]
        public async Task PermissionService_HandlesNullUser_Gracefully()
        {
            // Arrange
            SetupNullUser();

            // Act
            var hasPermission = await _permissionService.HasEditPermissionForServiceAsync(CardiologyServiceId);
            var requiredPermission = await _permissionService.GetRequiredPermissionForServiceAsync(CardiologyServiceId);

            // Assert
            Assert.False(hasPermission);
            Assert.Equal(CardiologyEditPermission, requiredPermission); // Should still return the required permission
        }

        [Fact]
        public async Task PermissionService_FallsBackToDefaultPermission_WhenServiceNotFound()
        {
            // Arrange
            var nonExistentServiceId = 999;
            SetupUserWithManagePermission();

            // Act
            var requiredPermission = await _permissionService.GetRequiredPermissionForServiceAsync(nonExistentServiceId);
            var hasPermission = await _permissionService.HasEditPermissionForServiceAsync(nonExistentServiceId);

            // Assert
            Assert.Equal(ClinicalSchedulePermissions.Manage, requiredPermission);
            Assert.True(hasPermission); // User has Manage permission
        }

        [Fact]
        public async Task EditOwnSchedulePermission_Integration()
        {
            // Arrange - User can only edit their own schedule
            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] { ClinicalSchedulePermissions.EditOwnSchedule });

            // Act - Check permission for own schedule vs others
            var ownScheduleResult = await _permissionService.CheckInstructorScheduleParamsAsync(
                mothraId: TestUserMothraId,
                rotationId: TestInstructorScheduleId,
                serviceId: null,
                weekId: null,
                startDate: null,
                endDate: null
            );

            var otherScheduleResult = await _permissionService.CheckInstructorScheduleParamsAsync(
                mothraId: "otheruser",
                rotationId: OtherInstructorScheduleId,
                serviceId: null,
                weekId: null,
                startDate: null,
                endDate: null
            );

            // Assert
            Assert.True(ownScheduleResult);
            Assert.False(otherScheduleResult);
        }

        [Fact]
        public async Task CompletePermissionFlow_FromControllerToService()
        {
            // Arrange - Simulate complete flow from controller perspective
            var serviceId = InternalMedicineServiceId;

            // Setup user with specific service permission using integration test patterns
            TestDataBuilder.IntegrationHelpers.AddMemberPermissions(RapsContext, TestUserMothraId, InternalMedicineEditPermission);
            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] { InternalMedicineEditPermission });

            // Act - Full permission check flow
            var requiredPermission = await _permissionService.GetRequiredPermissionForServiceAsync(serviceId);
            var hasPermission = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Simulate checking for a rotation in this service
            Context.Rotations.Add(new Rotation
            {
                RotId = 99,
                ServiceId = serviceId,
                Name = "IM Rotation",
                Abbreviation = "IM"
            });
            await Context.SaveChangesAsync();

            var rotationPermission = await _permissionService.HasEditPermissionForRotationAsync(99);

            // Assert - Complete flow works correctly
            Assert.Equal(InternalMedicineEditPermission, requiredPermission);
            Assert.True(hasPermission);
            Assert.True(rotationPermission);
        }

        [Fact]
        public async Task PermissionCaching_NotCausingStaleData()
        {
            // Arrange - Initial permission setup
            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] { CardiologyEditPermission });

            // Act - Check permission initially
            var firstCheck = await _permissionService.HasEditPermissionForServiceAsync(CardiologyServiceId);

            // Change permissions mid-test (simulating permission update)
            SetupUserWithPermissionsForIntegration(TestUserMothraId, new[] { ClinicalSchedulePermissions.Manage });

            // Check permission again
            var secondCheck = await _permissionService.HasEditPermissionForServiceAsync(SurgeryServiceId);

            // Assert - Permissions should reflect current state, not cached
            Assert.True(firstCheck);
            Assert.True(secondCheck); // Should now have access via Manage permission
        }
    }
}
