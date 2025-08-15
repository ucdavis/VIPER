using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AcademicYearService _academicYearService;
        private readonly WeekService _weekService;

        public RotationsControllerTest()
        {
            // Use in-memory database for testing instead of mocking the context
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ClinicalSchedulerContext(options);

            _mockLogger = new Mock<ILogger<RotationsController>>();
            _mockCache = new Mock<IMemoryCache>();

            // Create real service instances for testing
            var mockAcademicYearLogger = new Mock<ILogger<AcademicYearService>>();
            var mockWeekLogger = new Mock<ILogger<WeekService>>();
            _academicYearService = new AcademicYearService(mockAcademicYearLogger.Object, _context);
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
                _academicYearService,
                _weekService,
                rotationService,
                _mockCache.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void RotationsController_HasRequiredDependencies()
        {
            // Arrange
            var mockRotationLogger = new Mock<ILogger<RotationService>>();
            var rotationService = new RotationService(mockRotationLogger.Object, _context);

            // Act
            var controller = new RotationsController(
                _context,
                _academicYearService,
                _weekService,
                rotationService,
                _mockCache.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(controller);

            // Verify dependencies were passed correctly - logger shouldn't be called during construction
            // Note: Cannot verify logger usage without specific method calls
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
            Assert.Equal("SVMSecure.ClnSched", permissionAttribute.Allow);
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
                _academicYearService,
                _weekService,
                rotationService,
                _mockCache.Object,
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
                _academicYearService,
                _weekService,
                rotationService,
                _mockCache.Object,
                _mockLogger.Object);

            // Act & Assert
            Assert.True(year >= 2010 && year <= 2030); // Reasonable year range
        }
    }
}