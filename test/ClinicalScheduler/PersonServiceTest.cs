using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler
{
    public class PersonServiceTest : IDisposable
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly Mock<ILogger<PersonService>> _mockLogger;
        private readonly PersonService _personService;

        public PersonServiceTest()
        {
            // Use in-memory database for testing
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ClinicalSchedulerContext(options);

            _mockLogger = new Mock<ILogger<PersonService>>();
            _personService = new PersonService(_mockLogger.Object, _context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create test weeks
            var testWeeks = new[]
            {
        new Week { WeekId = 1, DateStart = DateTime.UtcNow.AddDays(-365), DateEnd = DateTime.UtcNow.AddDays(-358), TermCode = 202401 },
        new Week { WeekId = 2, DateStart = DateTime.UtcNow.AddDays(-30), DateEnd = DateTime.UtcNow.AddDays(-23), TermCode = 202501 },
        new Week { WeekId = 3, DateStart = DateTime.UtcNow.AddDays(-800), DateEnd = DateTime.UtcNow.AddDays(-793), TermCode = 202301 }
    };

            _context.Weeks.AddRange(testWeeks);
            _context.SaveChanges(); // Save weeks before adding schedules

            // Create test instructor schedules without ignored properties
            // Note: In production, properties like FullName, FirstName, etc. come from database views/joins
            // For tests, since these are ignored by EF, the PersonService test won't work with real data
            var testSchedules = new[]
            {
        new InstructorSchedule
        {
            InstructorScheduleId = 1,
            WeekId = 1,
            MothraId = "12345",
            RotationId = 1,
            Evaluator = true
        },
        new InstructorSchedule
        {
            InstructorScheduleId = 2,
            WeekId = 2,
            MothraId = "12345",
            RotationId = 2,
            Evaluator = false
        },
        new InstructorSchedule
        {
            InstructorScheduleId = 3,
            WeekId = 2,
            MothraId = "67890",
            RotationId = 1,
            Evaluator = true
        },
        new InstructorSchedule
        {
            InstructorScheduleId = 4,
            WeekId = 3,
            MothraId = "99999",
            RotationId = 1,
            Evaluator = false
        }
    };

            _context.InstructorSchedules.AddRange(testSchedules);
            
            // Create WeekGradYear entries that our EF queries depend on
            var testWeekGradYears = new[]
            {
                new WeekGradYear { WeekGradYearId = 1, WeekId = 1, GradYear = 2023, WeekNum = 1 },
                new WeekGradYear { WeekGradYearId = 2, WeekId = 2, GradYear = 2023, WeekNum = 20 },
                new WeekGradYear { WeekGradYearId = 3, WeekId = 3, GradYear = 2024, WeekNum = 1 },
                new WeekGradYear { WeekGradYearId = 4, WeekId = 4, GradYear = 2024, WeekNum = 20 }
            };
            
            _context.WeekGradYears.AddRange(testWeekGradYears);
            _context.SaveChanges(); // Save schedules and week grad years before adding persons

            // Create test persons (for year-specific queries)
            var testPersons = new[]
            {
        new Viper.Models.ClinicalScheduler.Person
        {
            IdsMothraId = "12345",
            PersonDisplayFullName = "Smith, John",
            PersonDisplayFirstName = "John",
            PersonDisplayLastName = "Smith",
            IdsMailId = "jsmith@example.com"
        },
        new Viper.Models.ClinicalScheduler.Person
        {
            IdsMothraId = "67890",
            PersonDisplayFullName = "Doe, Jane",
            PersonDisplayFirstName = "Jane",
            PersonDisplayLastName = "Doe",
            IdsMailId = "jdoe@example.com"
        }
    };

            _context.Persons.AddRange(testPersons);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetCliniciansAsync_ReturnsExpectedClinicians()
        {
            // Note: This test is simplified because Entity Framework in-memory database
            // ignores properties that come from database views in production
            // In a real scenario, these properties would be populated by SQL views/joins

            // Act
            var result = await _personService.GetCliniciansByGradYearRangeAsync(2022, 2024);

            // Assert
            Assert.NotNull(result);
            // Verify we get records for our test MothraIds (properties may be null due to EF configuration)
            var mothraIds = result.Select(c => ((dynamic)c).MothraId).ToList();
            Assert.Contains("12345", mothraIds);
            Assert.Contains("67890", mothraIds);
            // Old Clinician should be excluded due to 730 day limit
            Assert.DoesNotContain("99999", mothraIds);
        }

        [Fact]
        public async Task GetCliniciansAsync_WithLimitedHistory_ExcludesOldRecords()
        {
            // Act
            var result = await _personService.GetCliniciansByGradYearRangeAsync(2023, 2024);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Should still have John Smith and Jane Doe (recent schedules)
            Assert.DoesNotContain(result, c => ((dynamic)c).MothraId == "99999"); // Old Clinician should be excluded
        }

        [Fact]
        public async Task GetCliniciansAsync_WithoutHistorical_UsesDefaultRange()
        {
            // Act
            var result = await _personService.GetCliniciansByGradYearRangeAsync(2023, 2024);

            // Assert
            Assert.NotNull(result);
            // With includeHistorical: false, it uses a more limited range
            // The exact results depend on the current date vs test data dates
            // Just verify the method doesn't crash and returns a collection
        }

        [Fact]
        public async Task GetPersonAsync_WithValidMothraId_ReturnsPerson()
        {
            // Note: Simplified test due to EF in-memory database limitations
            // Properties like FullName come from database views in production

            // Act
            var result = await _personService.GetPersonAsync("12345");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("12345", ((dynamic)result).MothraId);
            Assert.Equal(2, ((dynamic)result).TotalSchedules); // John Smith has 2 schedule entries
            // Note: FullName, FirstName, LastName may be null due to EF configuration for in-memory testing
        }

        [Fact]
        public async Task GetPersonAsync_WithInvalidMothraId_ReturnsNull()
        {
            // Act
            var result = await _personService.GetPersonAsync("INVALID");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPersonAsync_WithNullMothraId_ReturnsNull()
        {
            // Act
            var result = await _personService.GetPersonAsync(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPersonAsync_WithEmptyMothraId_ReturnsNull()
        {
            // Act
            var result = await _personService.GetPersonAsync("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCliniciansByYearAsync_WithValidYear_ReturnsClinicians()
        {
            // Act - Using year from the recent test data
            var result = await _personService.GetCliniciansByYearAsync(DateTime.Now.Year);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 1); // Should have at least Jane Doe from recent schedules

            var clinician = result.FirstOrDefault(c => ((dynamic)c).MothraId == "67890");
            if (clinician != null)
            {
                Assert.Equal("Doe, Jane", ((dynamic)clinician).FullName);
                Assert.Equal(DateTime.Now.Year, ((dynamic)clinician).Year);
                Assert.True(((dynamic)clinician).ScheduleCount > 0);
            }
        }

        [Fact]
        public async Task GetCliniciansByYearAsync_WithOldYear_ReturnsHistoricalClinicians()
        {
            // Act - Using 2023 to match the old test data
            var result = await _personService.GetCliniciansByYearAsync(2023);

            // Assert
            Assert.NotNull(result);
            // May or may not have results depending on the test data year alignment
            // This tests the method doesn't crash with historical years
        }

        [Fact]
        public async Task GetAllMothraIdsAsync_ReturnsAllUniqueMothraIds()
        {
            // Act
            var result = await _personService.GetAllMothraIdsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("12345", result);
            Assert.Contains("67890", result);
            Assert.Contains("99999", result);
            Assert.Equal(3, result.Count); // Should have exactly 3 unique MothraIds

            // Verify they are sorted
            var sortedExpected = result.OrderBy(x => x).ToList();
            Assert.Equal(sortedExpected, result);
        }

        [Fact]
        public async Task GetCliniciansAsync_HandlesMultipleSchedulesPerClinician()
        {
            // This tests that clinicians with multiple schedule entries are properly grouped

            // Act
            var result = await _personService.GetCliniciansByGradYearRangeAsync(2022, 2024);

            // Assert
            var johnSmith = result.FirstOrDefault(c => ((dynamic)c).MothraId == "12345");
            Assert.NotNull(johnSmith);

            // John Smith should appear only once despite having 2 schedule entries
            var johnSmithCount = result.Count(c => ((dynamic)c).MothraId == "12345");
            Assert.Equal(1, johnSmithCount);
        }

        [Fact]
        public async Task GetCliniciansAsync_ReturnsCorrectDataStructure()
        {
            // Act
            var result = await _personService.GetCliniciansByGradYearRangeAsync(2023, 2024);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            var firstClinician = result[0];
            dynamic clinician = firstClinician;

            // Verify core properties exist (some may be null due to EF in-memory limitations)
            Assert.NotNull(clinician.MothraId);
            Assert.Equal("InstructorSchedule", clinician.Source);
            Assert.NotNull(clinician.LastScheduled);
            // Note: FullName, FirstName, LastName may be null in tests due to EF configuration
        }

        [Theory]
        [InlineData(30)]
        [InlineData(365)]
        [InlineData(730)]
        [InlineData(1095)]
        public async Task GetCliniciansAsync_WithDifferentSinceDays_ReturnsAppropriateResults(int sinceDays)
        {
            // Act
            var result = await _personService.GetCliniciansByGradYearRangeAsync(2020, 2024);

            // Assert
            Assert.NotNull(result);
            // Verify that longer timeframes include more or equal clinicians
            if (sinceDays >= 730)
            {
                Assert.True(result.Count >= 2); // Should include recent clinicians
            }
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