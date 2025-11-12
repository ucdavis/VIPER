using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.test.ClinicalScheduler
{
    public class PermissionsControllerTest : ClinicalSchedulerTestBase
    {
        private readonly Mock<ISchedulePermissionService> _mockPermissionService;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly Mock<ILogger<PermissionsController>> _mockLogger;
        private readonly PermissionsController _controller;

        public PermissionsControllerTest()
        {
            _mockPermissionService = new Mock<ISchedulePermissionService>();
            _mockGradYearService = new Mock<IGradYearService>();
            _mockLogger = new Mock<ILogger<PermissionsController>>();

            _controller = new PermissionsController(
                _mockPermissionService.Object,
                _mockGradYearService.Object,
                Context,
                _mockLogger.Object,
                MockUserHelper.Object
            );

            // Setup HttpContext with service provider for dependency injection
            var serviceProvider = TestDataBuilder.CreateTestServiceProvider(Context);
            TestDataBuilder.SetupControllerContext(_controller, serviceProvider);
        }

        [Fact]
        public async Task CanEditService_WithValidServiceAndPermission_ReturnsOkWithTrue()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            var serviceId = CardiologyServiceId;
            var canEdit = true;

            _mockPermissionService.Setup(s => s.HasEditPermissionForServiceAsync(serviceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(canEdit);

            // Act
            var result = await _controller.CanEditService(serviceId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value;
            Assert.NotNull(response);
            dynamic dynamicResponse = response;
            Assert.True(dynamicResponse.canEdit);
        }

        [Fact]
        public async Task CanEditService_WithValidServiceButNoPermission_ReturnsOkWithFalse()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            var serviceId = SurgeryServiceId;
            var canEdit = false;

            _mockPermissionService.Setup(s => s.HasEditPermissionForServiceAsync(serviceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(canEdit);

            // Act
            var result = await _controller.CanEditService(serviceId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value;
            Assert.NotNull(response);
            dynamic dynamicResponse = response;
            Assert.False(dynamicResponse.canEdit);
        }

        [Fact]
        public async Task CanEditRotation_WithValidRotationAndPermission_ReturnsOkWithTrue()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            var rotationId = CardiologyRotationId;
            var canEdit = true;

            _mockPermissionService.Setup(s => s.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(canEdit);

            // Act
            var result = await _controller.CanEditRotation(rotationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value;
            Assert.NotNull(response);
            dynamic dynamicResponse = response;
            Assert.True(dynamicResponse.canEdit);
        }

        [Fact]
        public async Task GetUserPermissions_WithValidUser_ReturnsOkWithPermissions()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            var servicePermissions = TestDataBuilder.ServicePermissionScenarios.CardiologyOnly;
            var editableServices = new List<Viper.Models.ClinicalScheduler.Service> { Context.Services.First(s => s.ServiceId == CardiologyServiceId) };

            _mockPermissionService.Setup(s => s.GetUserServicePermissionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(servicePermissions);
            _mockPermissionService.Setup(s => s.GetUserEditableServicesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(editableServices);

            // Act
            var result = await _controller.GetUserPermissions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            dynamic? response = okResult.Value;
            Assert.Equal(TestUserMothraId, response!.user.mothraId);
            Assert.Equal(TestUserDisplayName, response!.user.displayName);
            Assert.False(response!.permissions.hasManagePermission);
            Assert.Equal(1, response!.permissions.editableServiceCount);
            Assert.Single(response!.editableServices);
        }

        [Fact]
        public async Task CanEditService_WithException_ThrowsException()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            var serviceId = CardiologyServiceId;

            _mockPermissionService.Setup(s => s.HasEditPermissionForServiceAsync(serviceId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert - Controller throws exception, ApiExceptionFilter handles it in real scenarios
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.CanEditService(serviceId));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task CanEditRotation_WithException_ThrowsException()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            var rotationId = CardiologyRotationId;

            _mockPermissionService.Setup(s => s.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert - Controller throws exception, ApiExceptionFilter handles it in real scenarios
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.CanEditRotation(rotationId));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task GetUserPermissions_WithException_ThrowsException()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            _mockPermissionService.Setup(s => s.GetUserServicePermissionsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert - Controller throws exception, ApiExceptionFilter handles it in real scenarios
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.GetUserPermissions());
            Assert.Equal("Database error", exception.Message);
        }
    }
}
