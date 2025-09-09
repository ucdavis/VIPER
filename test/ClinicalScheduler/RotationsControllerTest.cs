using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.test.ClinicalScheduler
{
    public class RotationsControllerTest : ClinicalSchedulerTestBase
    {
        private readonly Mock<ILogger<RotationsController>> _mockLogger;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly Mock<IWeekService> _mockWeekService;
        private readonly Mock<IRotationService> _mockRotationService;
        private readonly Mock<ISchedulePermissionService> _mockPermissionService;
        private readonly Mock<IEvaluationPolicyService> _mockEvaluationPolicyService;
        private RotationsController _controller = null!;

        public RotationsControllerTest()
        {
            _mockLogger = new Mock<ILogger<RotationsController>>();
            _mockGradYearService = new Mock<IGradYearService>();
            _mockWeekService = new Mock<IWeekService>();
            _mockRotationService = new Mock<IRotationService>();
            _mockPermissionService = new Mock<ISchedulePermissionService>();
            _mockEvaluationPolicyService = new Mock<IEvaluationPolicyService>();

            SetupDefaultMockBehavior();
            RecreateController();
        }

        private void RecreateController()
        {
            _controller = new RotationsController(
                Context,
                _mockGradYearService.Object,
                _mockWeekService.Object,
                _mockRotationService.Object,
                _mockPermissionService.Object,
                _mockEvaluationPolicyService.Object,
                _mockLogger.Object);
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            TestDataBuilder.SetupControllerContext(_controller, serviceProvider);
        }

        private void SetupDefaultMockBehavior()
        {
            SetupMockRotations();
        }

        private void SetupMockRotations()
        {
            // Mock data: Two test rotations for permission filtering tests
            // - Cardiology (ID: CardiologyRotationId, Service: CardiologyServiceId) 
            // - Surgery (ID: SurgeryRotationId, Service: SurgeryServiceId)
            // Used to test that users see only rotations they have permissions for
            var allRotations = new List<RotationDto>
            {
                new RotationDto
                {
                    RotId = CardiologyRotationId,
                    Name = "Cardiology",
                    ServiceId = CardiologyServiceId,
                    Service = new ServiceDto { ServiceId = CardiologyServiceId, ServiceName = "Cardiology Service", ShortName = "CARD" }
                },
                new RotationDto
                {
                    RotId = SurgeryRotationId,
                    Name = "Surgery",
                    ServiceId = SurgeryServiceId,
                    Service = new ServiceDto { ServiceId = SurgeryServiceId, ServiceName = "Surgery Service", ShortName = "SURG" }
                }
            };
            _mockRotationService.Setup(s => s.GetRotationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(allRotations);
        }

        private void SetupMockPermissions(bool hasFullPermissions = false, int? allowedServiceId = null)
        {
            if (hasFullPermissions)
            {
                _mockPermissionService.Setup(p => p.HasEditPermissionForServiceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            }
            else if (allowedServiceId.HasValue)
            {
                _mockPermissionService.Setup(p => p.HasEditPermissionForServiceAsync(allowedServiceId.Value, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                _mockPermissionService.Setup(p => p.HasEditPermissionForServiceAsync(It.Is<int>(id => id != allowedServiceId.Value), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
            }
            else
            {
                _mockPermissionService.Setup(p => p.HasEditPermissionForServiceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
            }
        }

        private static IEnumerable<RotationDto> ExtractRotationsFromResult(ActionResult<IEnumerable<RotationDto>> result)
        {
            // Handle both implicit and explicit ActionResult patterns
            if (result.Result != null)
            {
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                return Assert.IsAssignableFrom<IEnumerable<RotationDto>>(okResult.Value);
            }
            else
            {
                return Assert.IsAssignableFrom<IEnumerable<RotationDto>>(result.Value);
            }
        }

        [Fact]
        public async Task GetRotations_ForUserWithFullPermissions_ReturnsAllRotations()
        {
            // Setup: User has edit permissions for all services
            SetupMockPermissions(hasFullPermissions: true);
            RecreateController();

            var result = await _controller.GetRotations();

            var rotations = ExtractRotationsFromResult(result);
            Assert.Equal(2, rotations.Count()); // Should return both Cardiology and Surgery rotations
        }

        [Fact]
        public async Task GetRotations_ForUserWithServicePermission_ReturnsFilteredRotations()
        {
            // Setup: User has edit permissions only for Cardiology service
            SetupMockPermissions(allowedServiceId: CardiologyServiceId);
            RecreateController();

            var result = await _controller.GetRotations();

            var rotations = ExtractRotationsFromResult(result);
            var rotationList = rotations.ToList();
            Assert.Single(rotationList); // Should return only Cardiology rotation
            Assert.Equal(CardiologyRotationId, rotationList[0].RotId);
        }

        [Fact]
        public async Task GetRotations_ForUserWithNoPermissions_ReturnsEmptyList()
        {
            // Setup: User has no edit permissions for any service
            SetupMockPermissions(); // Default is no permissions
            RecreateController();

            var result = await _controller.GetRotations();

            var rotations = ExtractRotationsFromResult(result);
            Assert.Empty(rotations); // Should return empty list when no permissions
        }
    }
}
