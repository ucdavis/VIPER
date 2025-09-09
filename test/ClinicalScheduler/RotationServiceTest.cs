using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler
{
    public class RotationServiceTest : IDisposable
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly Mock<ILogger<RotationService>> _mockLogger;
        private readonly RotationService _rotationService;

        public RotationServiceTest()
        {
            // Use in-memory database for testing with configuration to ignore navigation properties
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ClinicalSchedulerContext(options);

            _mockLogger = new Mock<ILogger<RotationService>>();
            _rotationService = new RotationService(_mockLogger.Object, _context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Simplified approach: Create basic test data without complex relationships
            // The actual service methods use Include() to handle navigation properties properly
            try
            {
                // Create test services first
                var testServices = new[]
                {
                    new Service { ServiceId = 1, ServiceName = "Anatomic Pathology", ShortName = "AP" },
                    new Service { ServiceId = 2, ServiceName = "Cardiology", ShortName = "CARD" },
                    new Service { ServiceId = 3, ServiceName = "Behavior", ShortName = "BEH" }
                };

                foreach (var service in testServices)
                {
                    _context.Services.Add(service);
                }
                _context.SaveChanges();

                // Create test rotations - focus on the deduplication use case
                var testRotations = new[]
                {
                    new Rotation { RotId = 101, Name = "Anatomic Pathology", Abbreviation = "AnatPath", SubjectCode = "VM", CourseNumber = "456", ServiceId = 1 },
                    new Rotation { RotId = 102, Name = "Cardiology", Abbreviation = "Card", SubjectCode = "VM", CourseNumber = "789", ServiceId = 2 },
                    new Rotation { RotId = 103, Name = "Behavior", Abbreviation = "Beh", SubjectCode = "VM", CourseNumber = "123", ServiceId = 3 },
                    new Rotation { RotId = 104, Name = "Advanced Anatomic Pathology", Abbreviation = "AdvAP", SubjectCode = "VM", CourseNumber = "457", ServiceId = 1 }
                };

                foreach (var rotation in testRotations)
                {
                    _context.Rotations.Add(rotation);
                }
                _context.SaveChanges();

                // Create minimal test weeks and schedules for the methods that need them
                var testWeeks = new[]
                {
                    new Week { WeekId = 1, DateStart = DateTime.Now.AddDays(-30), DateEnd = DateTime.Now.AddDays(-23), TermCode = 202501 },
                    new Week { WeekId = 2, DateStart = DateTime.Now.AddDays(-7), DateEnd = DateTime.Now, TermCode = 202501 }
                };

                foreach (var week in testWeeks)
                {
                    _context.Weeks.Add(week);
                }
                _context.SaveChanges();

                var testInstructorSchedules = new[]
                {
                    new InstructorSchedule
                    {
                        InstructorScheduleId = 1,
                        WeekId = 1,
                        MothraId = "12345",
                        RotationId = 101,
                        Evaluator = true
                    }
                };

                foreach (var schedule in testInstructorSchedules)
                {
                    _context.InstructorSchedules.Add(schedule);
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log the exception and provide a clear error message for debugging
                Console.WriteLine($"Error seeding test data: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task GetRotationsAsync_ReturnsExpectedRotations()
        {
            // Act
            var result = await _rotationService.GetRotationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);

            // Verify that each RotId appears only once (no duplicates)
            var rotIds = result.Select(r => r.RotId).ToList();
            var uniqueRotIds = rotIds.Distinct().ToList();
            Assert.Equal(rotIds.Count, uniqueRotIds.Count);

            // Verify specific rotations are present
            Assert.Contains(result, r => r.RotId == 101 && r.Name == "Anatomic Pathology");
            Assert.Contains(result, r => r.RotId == 102 && r.Name == "Cardiology");
            Assert.Contains(result, r => r.RotId == 103 && r.Name == "Behavior");
            Assert.Contains(result, r => r.RotId == 104 && r.Name == "Advanced Anatomic Pathology");
        }

        [Fact]
        public async Task GetRotationsAsync_IncludesServiceData()
        {
            // Act
            var result = await _rotationService.GetRotationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            // Verify Service navigation properties are loaded
            var anatomicPathRotation = result.FirstOrDefault(r => r.RotId == 101);
            Assert.NotNull(anatomicPathRotation);
            Assert.NotNull(anatomicPathRotation.Service);
            Assert.Equal("Anatomic Pathology", anatomicPathRotation.Service.ServiceName);
            Assert.Equal("AP", anatomicPathRotation.Service.ShortName);
        }

        [Fact]
        public async Task GetRotationsAsync_SortsCorrectly()
        {
            // Act
            var result = await _rotationService.GetRotationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            // Verify sorting: first by Service name, then by Rotation name
            for (int i = 0; i < result.Count - 1; i++)
            {
                var current = result[i];
                var next = result[i + 1];

                var currentServiceName = current.Service?.ServiceName ?? current.Name;
                var nextServiceName = next.Service?.ServiceName ?? next.Name;

                // Should be sorted by service name first
                Assert.True(string.Compare(currentServiceName, nextServiceName, StringComparison.Ordinal) <= 0);
            }
        }

        [Fact]
        public async Task GetRotationAsync_WithValidId_ReturnsRotation()
        {
            // Act
            var result = await _rotationService.GetRotationAsync(101);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(101, result.RotId);
            Assert.Equal("Anatomic Pathology", result.Name);
            Assert.Equal("AnatPath", result.Abbreviation);
            Assert.NotNull(result.Service);
            Assert.Equal("Anatomic Pathology", result.Service.ServiceName);
        }

        [Fact]
        public async Task GetRotationAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _rotationService.GetRotationAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRotationsByCourseAsync_FiltersCorrectly()
        {
            // Act
            var result = await _rotationService.GetRotationsByCourseAsync("456");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
            Assert.All(result, r => Assert.Equal("456", r.CourseNumber));

            // Should include Anatomic Pathology
            Assert.Contains(result, r => r.Name == "Anatomic Pathology");
        }

        [Fact]
        public async Task GetRotationsByCourseAsync_WithSubjectCode_FiltersCorrectly()
        {
            // Act
            var result = await _rotationService.GetRotationsByCourseAsync("456", "VM");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
            Assert.All(result, r => Assert.Equal("456", r.CourseNumber));
            Assert.All(result, r => Assert.Equal("VM", r.SubjectCode));
        }

        [Fact]
        public async Task GetRotationsByServiceAsync_FiltersCorrectly()
        {
            // Act
            var result = await _rotationService.GetRotationsByServiceAsync(1); // Anatomic Pathology service

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 2); // Should have both Anatomic Pathology rotations
            Assert.All(result, r => Assert.Equal(1, r.ServiceId));

            // Should include both anatomic pathology rotations
            Assert.Contains(result, r => r.Name == "Anatomic Pathology");
            Assert.Contains(result, r => r.Name == "Advanced Anatomic Pathology");
        }

        [Fact]
        public async Task GetServicesAsync_ReturnsAllServices()
        {
            // Act
            var result = await _rotationService.GetServicesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // Verify specific services
            Assert.Contains(result, s => s.ServiceName == "Anatomic Pathology");
            Assert.Contains(result, s => s.ServiceName == "Cardiology");
            Assert.Contains(result, s => s.ServiceName == "Behavior");

            // Verify sorting
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(string.Compare(result[i].ServiceName, result[i + 1].ServiceName, StringComparison.Ordinal) <= 0);
            }
        }

        [Fact]
        public async Task GetServiceAsync_WithValidId_ReturnsService()
        {
            // Act
            var result = await _rotationService.GetServiceAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ServiceId);
            Assert.Equal("Anatomic Pathology", result.ServiceName);
            Assert.Equal("AP", result.ShortName);
        }

        [Fact]
        public async Task GetServiceAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _rotationService.GetServiceAsync(999);

            // Assert
            Assert.Null(result);
        }


        [Fact]
        public async Task GetRotationsAsync_HandlesEmptyDatabase()
        {
            // Create a new context with no data
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var emptyContext = new ClinicalSchedulerContext(options);
            var emptyService = new RotationService(_mockLogger.Object, emptyContext);

            // Act
            var result = await emptyService.GetRotationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Act & Assert
            Assert.NotNull(_rotationService);
        }

        [Fact]
        public async Task GetRotationsAsync_HandlesUniqueRotIds()
        {
            // This test verifies that the service handles unique RotIds correctly

            // Act
            var result = await _rotationService.GetRotationsAsync();

            // Assert - Each RotId should appear exactly once
            var anatomicPathRotations = result.Where(r => r.RotId == 101).ToList();
            Assert.Single(anatomicPathRotations);

            var cardiologyRotations = result.Where(r => r.RotId == 102).ToList();
            Assert.Single(cardiologyRotations);

            var behaviorRotations = result.Where(r => r.RotId == 103).ToList();
            Assert.Single(behaviorRotations);

            var advancedRotations = result.Where(r => r.RotId == 104).ToList();
            Assert.Single(advancedRotations);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
        }
    }
}
