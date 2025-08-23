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
    }
}