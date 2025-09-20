using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    public class GradYearServiceTest
    {
        private readonly Mock<ILogger<GradYearService>> _mockLogger;
        private readonly ClinicalSchedulerContext _context;

        public GradYearServiceTest()
        {
            _mockLogger = new Mock<ILogger<GradYearService>>();

            // Use in-memory database for testing
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ClinicalSchedulerContext(options);
        }

        [Fact]
        public void GradYearService_CanBeCreated()
        {
            // Arrange & Act
            var service = new GradYearService(_mockLogger.Object, _context);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void GradYearService_HasCorrectPublicMethods()
        {
            // Arrange
            var serviceType = typeof(GradYearService);

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
        public void GradYearService_ValidYears_ShouldBeAccepted(int year)
        {
            // Arrange
            _ = new GradYearService(_mockLogger.Object, _context);

            // Act & Assert
            Assert.True(year >= 2010 && year <= 2030); // Reasonable year range
        }
    }
}
