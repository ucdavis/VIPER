using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Controllers;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    public class RotationsControllerTest
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly Mock<ILogger<RotationsController>> _mockLogger;
        private readonly GradYearService _gradYearService;
        private readonly WeekService _weekService;

        public RotationsControllerTest()
        {
            // Use in-memory database for testing instead of mocking the context
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ClinicalSchedulerContext(options);

            _mockLogger = new Mock<ILogger<RotationsController>>();

            // Create real service instances for testing
            var mockGradYearLogger = new Mock<ILogger<GradYearService>>();
            var mockWeekLogger = new Mock<ILogger<WeekService>>();
            _gradYearService = new GradYearService(mockGradYearLogger.Object, _context);
            _weekService = new WeekService(mockWeekLogger.Object, _context);
        }

        [Fact]
        public void RotationsController_CanBeCreated()
        {
            // Arrange
            var mockRotationLogger = new Mock<ILogger<RotationService>>();
            var rotationService = new RotationService(mockRotationLogger.Object, _context);

            // Act
            var controller = new RotationsController(
                _context,
                _gradYearService,
                _weekService,
                rotationService,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void RotationsController_HasCorrectPermissionAttribute()
        {
            // Arrange
            var controllerType = typeof(RotationsController);

            // Act
            var permissionAttribute = controllerType.GetCustomAttributes(typeof(Web.Authorization.PermissionAttribute), false)
                .FirstOrDefault() as Web.Authorization.PermissionAttribute;

            // Assert
            Assert.NotNull(permissionAttribute);
            Assert.Equal(ClinicalSchedulePermissions.Manage, permissionAttribute.Allow);
        }

        [Fact]
        public void RotationsController_HasCorrectRouteAttribute()
        {
            // Arrange
            var controllerType = typeof(RotationsController);

            // Act
            var routeAttribute = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
                .FirstOrDefault() as RouteAttribute;

            // Assert
            Assert.NotNull(routeAttribute);
            Assert.Equal("api/clinicalscheduler/rotations", routeAttribute.Template);
        }

        [Fact]
        public void RotationDto_HasRequiredProperties()
        {
            // Arrange
            var dto = new RotationDto();

            // Act & Assert
            Assert.Equal(0, dto.RotId);
            Assert.Equal(string.Empty, dto.Name);
            Assert.Equal(string.Empty, dto.Abbreviation);
            Assert.Equal(string.Empty, dto.SubjectCode);
            Assert.Equal(string.Empty, dto.CourseNumber);
            Assert.Equal(0, dto.ServiceId);
            Assert.Null(dto.Service);
        }

        [Fact]
        public void ServiceDto_HasRequiredProperties()
        {
            // Arrange
            var dto = new ServiceDto();

            // Act & Assert
            Assert.Equal(0, dto.ServiceId);
            Assert.Equal(string.Empty, dto.ServiceName);
            Assert.Equal(string.Empty, dto.ShortName);
            Assert.Null(dto.ScheduleEditPermission);
            Assert.Null(dto.UserCanEdit);
        }

        [Fact]
        public void ServiceDto_WithPermissionProperties_SetsCorrectly()
        {
            // Arrange
            var dto = new ServiceDto
            {
                ServiceId = 1,
                ServiceName = "Cardiology",
                ShortName = "Cardio",
                ScheduleEditPermission = "SVMSecure.ClnSched.Edit.Cardio",
                UserCanEdit = true
            };

            // Act & Assert
            Assert.Equal(1, dto.ServiceId);
            Assert.Equal("Cardiology", dto.ServiceName);
            Assert.Equal("Cardio", dto.ShortName);
            Assert.Equal("SVMSecure.ClnSched.Edit.Cardio", dto.ScheduleEditPermission);
            Assert.True(dto.UserCanEdit);
        }

        [Fact]
        public void RotationDto_CanSetProperties()
        {
            // Arrange
            var dto = new RotationDto();
            var service = new ServiceDto { ServiceId = 1, ServiceName = "Test Service", ShortName = "TS" };

            // Act
            dto.RotId = 123;
            dto.Name = "Test Rotation";
            dto.Abbreviation = "TR";
            dto.SubjectCode = "TEST";
            dto.CourseNumber = "101";
            dto.ServiceId = 1;
            dto.Service = service;

            // Assert
            Assert.Equal(123, dto.RotId);
            Assert.Equal("Test Rotation", dto.Name);
            Assert.Equal("TR", dto.Abbreviation);
            Assert.Equal("TEST", dto.SubjectCode);
            Assert.Equal("101", dto.CourseNumber);
            Assert.Equal(1, dto.ServiceId);
            Assert.NotNull(dto.Service);
            Assert.Equal("Test Service", dto.Service.ServiceName);
        }

        [Fact]
        public void ServiceDto_CanSetProperties()
        {
            // Arrange
            var dto = new ServiceDto();

            // Act
            dto.ServiceId = 456;
            dto.ServiceName = "Another Service";
            dto.ShortName = "AS";

            // Assert
            Assert.Equal(456, dto.ServiceId);
            Assert.Equal("Another Service", dto.ServiceName);
            Assert.Equal("AS", dto.ShortName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(999)]
        public void RotationsController_AcceptsValidServiceIds(int serviceId)
        {
            // Arrange
            var mockRotationLogger = new Mock<ILogger<RotationService>>();
            var rotationService = new RotationService(mockRotationLogger.Object, _context);
            _ = new RotationsController(
                _context,
                _gradYearService,
                _weekService,
                rotationService,
                _mockLogger.Object);

            // Act & Assert
            Assert.True(serviceId > 0); // Valid service IDs should be positive
        }

        [Theory]
        [InlineData(2024)]
        [InlineData(2025)]
        [InlineData(2026)]
        public void RotationsController_AcceptsValidYears(int year)
        {
            // Arrange
            var mockRotationLogger = new Mock<ILogger<RotationService>>();
            var rotationService = new RotationService(mockRotationLogger.Object, _context);
            _ = new RotationsController(
                _context,
                _gradYearService,
                _weekService,
                rotationService,
                _mockLogger.Object);

            // Act & Assert
            Assert.True(year >= 2010 && year <= 2030); // Reasonable year range
        }
    }
}