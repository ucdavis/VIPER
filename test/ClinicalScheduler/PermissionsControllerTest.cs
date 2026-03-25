using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.test.ClinicalScheduler
{
    public class PermissionsControllerTest : ClinicalSchedulerTestBase
    {
        private readonly ISchedulePermissionService _mockPermissionService;
        private readonly IGradYearService _mockGradYearService;
        private readonly ILogger<PermissionsController> _mockLogger;
        private readonly PermissionsController _controller;

        public PermissionsControllerTest()
        {
            _mockPermissionService = Substitute.For<ISchedulePermissionService>();
            _mockGradYearService = Substitute.For<IGradYearService>();
            _mockLogger = Substitute.For<ILogger<PermissionsController>>();

            _controller = new PermissionsController(
                _mockPermissionService,
                _mockGradYearService,
                Context,
                _mockLogger,
                MockUserHelper
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
            MockUserHelper.GetCurrentUser().Returns(testUser);

            var serviceId = CardiologyServiceId;
            var canEdit = true;

            _mockPermissionService.HasEditPermissionForServiceAsync(serviceId, Arg.Any<CancellationToken>())
                .Returns(canEdit);

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
            MockUserHelper.GetCurrentUser().Returns(testUser);

            var serviceId = SurgeryServiceId;
            var canEdit = false;

            _mockPermissionService.HasEditPermissionForServiceAsync(serviceId, Arg.Any<CancellationToken>())
                .Returns(canEdit);

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
            MockUserHelper.GetCurrentUser().Returns(testUser);

            var rotationId = CardiologyRotationId;
            var canEdit = true;

            _mockPermissionService.HasEditPermissionForRotationAsync(rotationId, Arg.Any<CancellationToken>())
                .Returns(canEdit);

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
            MockUserHelper.GetCurrentUser().Returns(testUser);

            var servicePermissions = TestDataBuilder.ServicePermissionScenarios.CardiologyOnly;
            var editableServices = new List<Viper.Models.ClinicalScheduler.Service> { Context.Services.First(s => s.ServiceId == CardiologyServiceId) };

            _mockPermissionService.GetUserServicePermissionsAsync(Arg.Any<CancellationToken>())
                .Returns(servicePermissions);
            _mockPermissionService.GetUserEditableServicesAsync(Arg.Any<CancellationToken>())
                .Returns(editableServices);

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
            MockUserHelper.GetCurrentUser().Returns(testUser);

            var serviceId = CardiologyServiceId;

            _mockPermissionService.HasEditPermissionForServiceAsync(serviceId, Arg.Any<CancellationToken>())
                .Throws(new Exception("Database error"));

            // Act & Assert - Controller throws exception, ApiExceptionFilter handles it in real scenarios
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.CanEditService(serviceId));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task CanEditRotation_WithException_ThrowsException()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.GetCurrentUser().Returns(testUser);

            var rotationId = CardiologyRotationId;

            _mockPermissionService.HasEditPermissionForRotationAsync(rotationId, Arg.Any<CancellationToken>())
                .Throws(new Exception("Database error"));

            // Act & Assert - Controller throws exception, ApiExceptionFilter handles it in real scenarios
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.CanEditRotation(rotationId));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task GetUserPermissions_WithException_ThrowsException()
        {
            // Arrange
            var testUser = CreateTestUser();
            MockUserHelper.GetCurrentUser().Returns(testUser);

            _mockPermissionService.GetUserServicePermissionsAsync(Arg.Any<CancellationToken>())
                .Throws(new Exception("Database error"));

            // Act & Assert - Controller throws exception, ApiExceptionFilter handles it in real scenarios
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.GetUserPermissions());
            Assert.Equal("Database error", exception.Message);
        }
    }
}
