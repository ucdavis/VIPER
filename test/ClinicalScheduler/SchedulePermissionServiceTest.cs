using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.test.ClinicalScheduler
{
    public class SchedulePermissionServiceTest : ClinicalSchedulerTestBase
    {
        private readonly Mock<ILogger<SchedulePermissionService>> _mockLogger;
        private readonly SchedulePermissionService _permissionService;

        public SchedulePermissionServiceTest()
        {
            _mockLogger = new Mock<ILogger<SchedulePermissionService>>();
            _permissionService = new SchedulePermissionService(Context, MockRapsContext.Object, _mockLogger.Object, MockUserHelper.Object);
        }

        [Fact]
        public async Task GetRequiredPermissionForServiceAsync_WithCustomPermission_ReturnsCustomPermission()
        {
            // Arrange
            var serviceId = CardiologyServiceId; // Service with custom permission

            // Act
            var result = await _permissionService.GetRequiredPermissionForServiceAsync(serviceId);

            // Assert
            Assert.Equal(CardiologyEditPermission, result);
        }

        [Fact]
        public async Task GetRequiredPermissionForServiceAsync_WithNullPermission_ReturnsDefaultPermission()
        {
            // Arrange
            var serviceId = SurgeryServiceId; // Service with null permission

            // Act
            var result = await _permissionService.GetRequiredPermissionForServiceAsync(serviceId);

            // Assert
            Assert.Equal(ClinicalSchedulePermissions.Manage, result);
        }

        [Fact]
        public async Task GetRequiredPermissionForServiceAsync_WithEmptyPermission_ReturnsDefaultPermission()
        {
            // Arrange
            var serviceId = EmergencyMedicineServiceId; // Service with empty string permission

            // Act
            var result = await _permissionService.GetRequiredPermissionForServiceAsync(serviceId);

            // Assert
            Assert.Equal(ClinicalSchedulePermissions.Manage, result);
        }

        [Fact]
        public async Task GetRequiredPermissionForServiceAsync_WithNonExistentService_ReturnsDefaultPermission()
        {
            // Arrange
            var nonExistentServiceId = 999;

            // Act
            var result = await _permissionService.GetRequiredPermissionForServiceAsync(nonExistentServiceId);

            // Assert
            Assert.Equal(ClinicalSchedulePermissions.Manage, result);
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithManagePermission_ReturnsTrue()
        {
            // Arrange
            var serviceId = CardiologyServiceId;
            SetupUserWithManagePermission(TestUserMothraId, true);

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithSpecificServicePermission_ReturnsTrue()
        {
            // Arrange
            var serviceId = CardiologyServiceId; // User has permission for this service
            SetupUserWithoutManagePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithoutPermission_ReturnsFalse()
        {
            // Arrange
            var serviceId = SurgeryServiceId; // User doesn't have permission for this service
            SetupUserWithoutManagePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasEditPermissionForRotationAsync_WithManagePermission_ReturnsTrue()
        {
            // Arrange
            var rotationId = CardiologyRotationId; // Cardiology rotation (serviceId = 1)
            SetupUserWithManagePermission(TestUserMothraId, true);

            // Act
            var result = await _permissionService.HasEditPermissionForRotationAsync(rotationId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasEditPermissionForRotationAsync_WithSpecificServicePermission_ReturnsTrue()
        {
            // Arrange
            var rotationId = CardiologyRotationId; // Cardiology rotation (serviceId = 1)
            SetupUserWithoutManagePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.HasEditPermissionForRotationAsync(rotationId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasEditPermissionForRotationAsync_WithoutPermission_ReturnsFalse()
        {
            // Arrange
            var rotationId = SurgeryRotationId; // Surgery rotation (serviceId = 2)
            SetupUserWithoutManagePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.HasEditPermissionForRotationAsync(rotationId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasEditPermissionForRotationAsync_WithNonExistentRotation_ReturnsFalse()
        {
            // Arrange
            var rotationId = 999; // Non-existent rotation
            SetupUserWithManagePermission(TestUserMothraId, true);

            // Act
            var result = await _permissionService.HasEditPermissionForRotationAsync(rotationId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetUserEditableServicesAsync_WithManagePermission_ReturnsAllServices()
        {
            // Arrange
            SetupUserWithManagePermission(TestUserMothraId, true);

            // Act
            var result = await _permissionService.GetUserEditableServicesAsync();

            // Assert
            Assert.Equal(4, result.Count); // All services should be returned
        }

        [Fact]
        public async Task GetUserEditableServicesAsync_WithoutManagePermission_ReturnsFilteredServices()
        {
            // Arrange
            SetupUserWithoutManagePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.GetUserEditableServicesAsync();

            // Assert
            // Should return only services where user has specific permissions
            // For this test, we'll mock that user only has permission for Cardiology
            Assert.Single(result);
            Assert.Equal("Cardiology", result[0].ServiceName);
        }

        [Fact]
        public async Task GetUserServicePermissionsAsync_WithManagePermission_ReturnsAllTrue()
        {
            // Arrange
            SetupUserWithManagePermission(TestUserMothraId, true);

            // Act
            var result = await _permissionService.GetUserServicePermissionsAsync();

            // Assert
            Assert.Equal(4, result.Count); // All services should be present
            Assert.True(result[CardiologyServiceId]); // Cardiology - user has manage permission
            Assert.True(result[SurgeryServiceId]); // Surgery - user has manage permission
            Assert.True(result[InternalMedicineServiceId]); // Internal Medicine - user has manage permission
            Assert.True(result[EmergencyMedicineServiceId]); // Emergency Medicine - user has manage permission
        }

        [Fact]
        public async Task GetUserServicePermissionsAsync_WithoutManagePermission_ReturnsPartialPermissions()
        {
            // Arrange
            SetupUserWithoutManagePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.GetUserServicePermissionsAsync();

            // Assert
            Assert.Equal(4, result.Count); // All services should be present
            Assert.True(result[CardiologyServiceId]); // Cardiology - user has permission
            Assert.False(result[SurgeryServiceId]); // Surgery - user doesn't have permission
            Assert.False(result[InternalMedicineServiceId]); // Internal Medicine - user doesn't have permission
            Assert.False(result[EmergencyMedicineServiceId]); // Emergency Medicine - user doesn't have permission
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithNullUser_ReturnsFalse()
        {
            // Arrange
            var serviceId = CardiologyServiceId;
            SetupNullUser();

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithAdminPermission_ReturnsTrue()
        {
            // Arrange
            var serviceId = CardiologyServiceId;
            SetupUserWithAdminPermission(TestUserMothraId);

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithEditClnSchedulesPermission_ReturnsTrue()
        {
            // Arrange
            var serviceId = CardiologyServiceId;
            SetupUserWithEditClnSchedulesPermission(TestUserMothraId);

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithServiceSpecificPermission_ForMatchingService_ReturnsTrue()
        {
            // Arrange
            var serviceId = CardiologyServiceId; // Service with specific permission "SVMSecure.ClnSched.Edit.Cardio"
            SetupUserWithServiceSpecificPermission(TestUserMothraId, CardiologyEditPermission);

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasEditPermissionForServiceAsync_WithServiceSpecificPermission_ForDifferentService_ReturnsFalse()
        {
            // Arrange
            var serviceId = SurgeryServiceId; // Surgery service, but user only has Cardiology permission
            SetupUserWithServiceSpecificPermission(TestUserMothraId, CardiologyEditPermission);

            // Act
            var result = await _permissionService.HasEditPermissionForServiceAsync(serviceId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetUserEditableServicesAsync_WithAdminPermission_ReturnsAllServices()
        {
            // Arrange
            SetupUserWithAdminPermission(TestUserMothraId);

            // Act
            var result = await _permissionService.GetUserEditableServicesAsync();

            // Assert
            Assert.Equal(4, result.Count); // All services should be returned
        }

        [Fact]
        public async Task GetUserEditableServicesAsync_WithEditClnSchedulesPermission_ReturnsAllServices()
        {
            // Arrange
            SetupUserWithEditClnSchedulesPermission(TestUserMothraId);

            // Act
            var result = await _permissionService.GetUserEditableServicesAsync();

            // Assert
            Assert.Equal(4, result.Count); // All services should be returned
        }

        [Fact]
        public async Task GetUserServicePermissionsAsync_WithAdminPermission_ReturnsAllTrue()
        {
            // Arrange
            SetupUserWithAdminPermission(TestUserMothraId);

            // Act
            var result = await _permissionService.GetUserServicePermissionsAsync();

            // Assert
            Assert.Equal(4, result.Count); // All services should be present
            Assert.All(result.Values, permission => Assert.True(permission)); // All should be true
        }

        [Fact]
        public async Task GetUserServicePermissionsAsync_WithEditClnSchedulesPermission_ReturnsAllTrue()
        {
            // Arrange
            SetupUserWithEditClnSchedulesPermission(TestUserMothraId);

            // Act
            var result = await _permissionService.GetUserServicePermissionsAsync();

            // Assert
            Assert.Equal(4, result.Count); // All services should be present
            Assert.All(result.Values, permission => Assert.True(permission)); // All should be true
        }

        [Fact]
        public async Task CanEditOwnScheduleAsync_WithEditOwnSchedulePermission_WhenOwnSchedule_ReturnsTrue()
        {
            // Arrange
            var instructorScheduleId = TestInstructorScheduleId; // Schedule belongs to TestUserMothraId
            SetupUserWithEditOwnSchedulePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.CanEditOwnScheduleAsync(instructorScheduleId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanEditOwnScheduleAsync_WithEditOwnSchedulePermission_WhenNotOwnSchedule_ReturnsFalse()
        {
            // Arrange
            var instructorScheduleId = OtherInstructorScheduleId; // Schedule belongs to different user
            SetupUserWithEditOwnSchedulePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.CanEditOwnScheduleAsync(instructorScheduleId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanEditOwnScheduleAsync_WithoutEditOwnSchedulePermission_ReturnsFalse()
        {
            // Arrange
            var instructorScheduleId = TestInstructorScheduleId;
            SetupUserWithoutEditOwnSchedulePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.CanEditOwnScheduleAsync(instructorScheduleId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanEditOwnScheduleAsync_WithNonExistentSchedule_ReturnsFalse()
        {
            // Arrange
            var instructorScheduleId = 999; // Non-existent schedule
            SetupUserWithEditOwnSchedulePermission(TestUserMothraId);

            // Act
            var result = await _permissionService.CanEditOwnScheduleAsync(instructorScheduleId);

            // Assert
            Assert.False(result);
        }

        // Helper methods for permission setups

        private void SetupUserWithAdminPermission(string mothraId)
        {
            var user = TestDataBuilder.CreateUser(mothraId);
            MockUserHelper.Setup(u => u.GetCurrentUser()).Returns(user);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Admin)).Returns(true);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Manage)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditClnSchedules)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditOwnSchedule)).Returns(false);
        }

        private void SetupUserWithEditClnSchedulesPermission(string mothraId)
        {
            var user = TestDataBuilder.CreateUser(mothraId);
            MockUserHelper.Setup(u => u.GetCurrentUser()).Returns(user);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Admin)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Manage)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditClnSchedules)).Returns(true);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditOwnSchedule)).Returns(false);
        }

        private void SetupUserWithServiceSpecificPermission(string mothraId, string specificPermission)
        {
            var user = TestDataBuilder.CreateUser(mothraId);
            MockUserHelper.Setup(u => u.GetCurrentUser()).Returns(user);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Admin)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Manage)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditClnSchedules)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditOwnSchedule)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, specificPermission)).Returns(true);
        }

        private void SetupUserWithEditOwnSchedulePermission(string mothraId)
        {
            var user = TestDataBuilder.CreateUser(mothraId);
            MockUserHelper.Setup(u => u.GetCurrentUser()).Returns(user);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Admin)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Manage)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditClnSchedules)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditOwnSchedule)).Returns(true);
        }

        private void SetupUserWithoutEditOwnSchedulePermission(string mothraId)
        {
            var user = TestDataBuilder.CreateUser(mothraId);
            MockUserHelper.Setup(u => u.GetCurrentUser()).Returns(user);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Admin)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.Manage)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditClnSchedules)).Returns(false);
            MockUserHelper.Setup(u => u.HasPermission(MockRapsContext.Object, user, ClinicalSchedulePermissions.EditOwnSchedule)).Returns(false);
        }
    }
}
