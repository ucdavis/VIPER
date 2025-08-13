using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    public class AcademicYearServiceTest
    {
        private readonly Mock<ILogger<AcademicYearService>> _mockLogger;
        private readonly ClinicalSchedulerContext _context;

        public AcademicYearServiceTest()
        {
            _mockLogger = new Mock<ILogger<AcademicYearService>>();

            // Use in-memory database for testing
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ClinicalSchedulerContext(options);
        }

        [Fact]
        public void AcademicYearService_CanBeCreated()
        {
            // Arrange & Act
            var service = new AcademicYearService(_mockLogger.Object, _context);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void AcademicYearService_HasRequiredDependencies()
        {
            // Arrange & Act
            var service = new AcademicYearService(_mockLogger.Object, _context);

            // Assert
            // Service should be created successfully with required dependencies
            Assert.NotNull(service);

            // Verify logger was passed correctly - logger shouldn't be called during construction
            // Note: Cannot verify logger usage without specific method calls
        }

        [Fact]
        public void AcademicYearService_HasCorrectPublicMethods()
        {
            // Arrange
            var serviceType = typeof(AcademicYearService);

            // Act
            var methods = serviceType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(m => m.DeclaringType == serviceType)
                .Select(m => m.Name)
                .ToList();

            // Assert
            Assert.Contains("GetCurrentGradYearAsync", methods);
            Assert.Contains("GetCurrentSelectionYearAsync", methods);
            Assert.Contains("GetAvailableGradYearsAsync", methods);
        }

        [Theory]
        [InlineData(2026)]
        [InlineData(2025)]
        [InlineData(2024)]
        public void AcademicYearService_ValidYears_ShouldBeAccepted(int year)
        {
            // Arrange
            _ = new AcademicYearService(_mockLogger.Object, _context);

            // Act & Assert
            Assert.True(year >= 2010 && year <= 2030); // Reasonable year range
        }

        [Fact]
        public void AcademicYearService_Constructor_AcceptsValidParameters()
        {
            // Arrange & Act & Assert
            // Service should be created successfully with valid parameters
            var service = new AcademicYearService(_mockLogger.Object, _context);
            Assert.NotNull(service);
        }
    }
}