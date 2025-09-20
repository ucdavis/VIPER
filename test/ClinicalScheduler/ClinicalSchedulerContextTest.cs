using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    public class ClinicalSchedulerContextTest
    {
        private static DbContextOptions<ClinicalSchedulerContext> GetInMemoryDbOptions()
        {
            return new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public void ClinicalSchedulerContext_CanBeCreated()
        {
            // Arrange
            var options = GetInMemoryDbOptions();

            // Act & Assert - Should not throw any exceptions
            using var context = new ClinicalSchedulerContext(options);
            Assert.NotNull(context);
        }

        [Fact]
        public void ClinicalSchedulerContext_ModelCanBeBuilt()
        {
            // Arrange
            var options = GetInMemoryDbOptions();

            // Act & Assert - Should not throw exceptions about missing primary keys
            using var context = new ClinicalSchedulerContext(options);

            // This will trigger model building and should not throw the CourseSessionOffering error
            var model = context.Model;
            Assert.NotNull(model);
        }

        [Fact]
        public void ClinicalSchedulerContext_HasExpectedDbSets()
        {
            // Arrange
            var options = GetInMemoryDbOptions();

            // Act
            using var context = new ClinicalSchedulerContext(options);

            // Assert - Verify all expected DbSets are present
            Assert.NotNull(context.Rotations);
            Assert.NotNull(context.Services);
            Assert.NotNull(context.InstructorSchedules);
            Assert.NotNull(context.Weeks);
            Assert.NotNull(context.WeekGradYears);
            // ScheduleAudits will be added in Phase 7
        }

        [Fact]
        public void ClinicalSchedulerContext_CanQueryRotations()
        {
            // Arrange
            var options = GetInMemoryDbOptions();

            // Act & Assert - Should not throw exceptions when querying
            using var context = new ClinicalSchedulerContext(options);

            // This should work without throwing CourseSessionOffering errors
            var query = context.Rotations.Include(r => r.Service);
            Assert.NotNull(query);

            // Execute the query (will be empty in in-memory database but should not error)
            var rotations = query.ToList();
            Assert.NotNull(rotations);
        }

        [Fact]
        public void ClinicalSchedulerContext_ServiceIgnoredPropertiesNotIncluded()
        {
            // Arrange
            var options = GetInMemoryDbOptions();

            // Act
            using var context = new ClinicalSchedulerContext(options);
            var serviceEntityType = context.Model.FindEntityType(typeof(Viper.Models.ClinicalScheduler.Service));

            // Assert - Verify that Encounters and Epas navigation properties are ignored
            Assert.NotNull(serviceEntityType);

            var encountersNavigation = serviceEntityType.FindNavigation("Encounters");
            var epasNavigation = serviceEntityType.FindNavigation("Epas");

            // These should be null because we ignored them in the configuration
            Assert.Null(encountersNavigation);
            Assert.Null(epasNavigation);

            // But Rotations navigation should still exist
            var rotationsNavigation = serviceEntityType.FindNavigation("Rotations");
            Assert.NotNull(rotationsNavigation);
        }
    }
}
