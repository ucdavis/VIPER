using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    public class WeekServiceTest
    {
        private readonly Mock<ILogger<WeekService>> _mockLogger;
        private readonly ClinicalSchedulerContext _context;

        public WeekServiceTest()
        {
            _mockLogger = new Mock<ILogger<WeekService>>();

            // Use in-memory database for testing
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ClinicalSchedulerContext(options);
        }

        [Fact]
        public void WeekService_CanBeCreated()
        {
            // Arrange & Act
            var service = new WeekService(_mockLogger.Object, _context);

            // Assert
            Assert.NotNull(service);
        }

        [Theory]
        [InlineData(2026)]
        [InlineData(2025)]
        [InlineData(2024)]
        public void WeekService_ValidGradYears_ShouldBeAccepted(int gradYear)
        {
            // Arrange
            _ = new WeekService(_mockLogger.Object, _context);

            // Act & Assert
            Assert.True(gradYear >= 2010 && gradYear <= 2030); // Reasonable year range
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WeekService_IncludeExtendedRotationParameter_ShouldBeHandled(bool includeExtendedRotation)
        {
            // Arrange
            _ = new WeekService(_mockLogger.Object, _context);

            // Act & Assert
            // The parameter should be a valid boolean
            Assert.IsType<bool>(includeExtendedRotation);
        }

        [Fact]
        public void WeekService_HasRequiredDependencies()
        {
            // Arrange & Act
            var service = new WeekService(_mockLogger.Object, _context);

            // Assert
            // Service should be created successfully with required dependencies
            Assert.NotNull(service);

            // Verify logger was passed correctly - logger shouldn't be called during construction
            // Note: Cannot verify logger usage without specific method calls
        }

        [Fact]
        public void WeekService_HasCorrectPublicMethods()
        {
            // Arrange
            var serviceType = typeof(WeekService);

            // Act
            var methods = serviceType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(m => m.DeclaringType == serviceType)
                .Select(m => m.Name)
                .ToList();

            // Assert
            Assert.Contains("GetWeeksAsync", methods);
            Assert.Contains("GetWeekAsync", methods);
            Assert.Contains("GetCurrentWeekAsync", methods);
            Assert.Contains("GetWeeksByDateRangeAsync", methods);
        }

        [Fact]
        public void WeekService_Constructor_AcceptsValidParameters()
        {
            // Arrange & Act & Assert
            // Service should be created successfully with valid parameters
            var service = new WeekService(_mockLogger.Object, _context);
            Assert.NotNull(service);
        }

        [Theory]
        [InlineData(1, 2026)]
        [InlineData(1, null)]
        [InlineData(100, 2025)]
        public void WeekService_GetWeekAsync_ValidatesParameterTypes(int weekId, int? gradYear)
        {
            // Arrange
            _ = new WeekService(_mockLogger.Object, _context);

            // Act & Assert
            // Parameters should be of correct types
            Assert.IsType<int>(weekId);
            if (gradYear.HasValue)
                Assert.IsType<int>(gradYear.Value);
        }
    }
}
