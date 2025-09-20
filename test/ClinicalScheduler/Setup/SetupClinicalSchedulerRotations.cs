using MockQueryable.Moq;
using Moq;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler.Setup
{
    /// <summary>
    /// Test data setup for Rotation entities in Clinical Scheduler tests
    /// </summary>
    internal static class SetupClinicalSchedulerRotations
    {
        public static readonly List<Rotation> TestRotations = new()
        {
            new Rotation
            {
                RotId = 1,
                ServiceId = 1,
                Name = "Cardiology Rotation",
                Abbreviation = "CARD"
            },
            new Rotation
            {
                RotId = 2,
                ServiceId = 2,
                Name = "Surgery Rotation",
                Abbreviation = "SURG"
            },
            new Rotation
            {
                RotId = 3,
                ServiceId = 3,
                Name = "Internal Medicine Rotation",
                Abbreviation = "IM"
            }
        };

        public static readonly List<WeekGradYear> TestWeekGradYears = new()
        {
            new WeekGradYear
            {
                WeekId = 1,
                GradYear = 2024,
                Week = new Week
                {
                    WeekId = 1,
                    DateStart = DateTime.UtcNow.AddDays(-70),
                    DateEnd = DateTime.UtcNow.AddDays(-64),
                    TermCode = 202401
                }
            },
            new WeekGradYear
            {
                WeekId = 2,
                GradYear = 2024,
                Week = new Week
                {
                    WeekId = 2,
                    DateStart = DateTime.UtcNow.AddDays(-63),
                    DateEnd = DateTime.UtcNow.AddDays(-57),
                    TermCode = 202401
                }
            },
            new WeekGradYear
            {
                WeekId = 1,
                GradYear = 2025,
                Week = new Week
                {
                    WeekId = 1,
                    DateStart = DateTime.UtcNow.AddDays(-7),
                    DateEnd = DateTime.UtcNow.AddDays(-1),
                    TermCode = 202501
                }
            },
            new WeekGradYear
            {
                WeekId = 2,
                GradYear = 2025,
                Week = new Week
                {
                    WeekId = 2,
                    DateStart = DateTime.UtcNow.AddDays(0),
                    DateEnd = DateTime.UtcNow.AddDays(6),
                    TermCode = 202501
                }
            }
        };

        /// <summary>
        /// Gets test rotations as queryable for mocking
        /// </summary>
        public static IQueryable<Rotation> GetTestRotations()
        {
            return TestRotations.AsQueryable();
        }

        /// <summary>
        /// Gets test week grad years as queryable for mocking
        /// </summary>
        public static IQueryable<WeekGradYear> GetTestWeekGradYears()
        {
            return TestWeekGradYears.AsQueryable();
        }

        /// <summary>
        /// Sets up the Rotations DbSet mock with test data
        /// </summary>
        public static void SetupRotationsTable(Mock<ClinicalSchedulerContext> mockContext)
        {
            var mockDbSet = TestRotations.AsQueryable().BuildMockDbSet();
            mockContext.Setup(c => c.Rotations).Returns(mockDbSet.Object);
        }

        /// <summary>
        /// Sets up the Rotations DbSet mock with custom test data
        /// </summary>
        public static void SetupRotationsTable(Mock<ClinicalSchedulerContext> mockContext, List<Rotation> customRotations)
        {
            var mockDbSet = customRotations.AsQueryable().BuildMockDbSet();
            mockContext.Setup(c => c.Rotations).Returns(mockDbSet.Object);
        }

        /// <summary>
        /// Sets up the WeekGradYears DbSet mock with test data
        /// </summary>
        public static void SetupWeekGradYearsTable(Mock<ClinicalSchedulerContext> mockContext)
        {
            var mockDbSet = TestWeekGradYears.AsQueryable().BuildMockDbSet();
            mockContext.Setup(c => c.WeekGradYears).Returns(mockDbSet.Object);
        }

        /// <summary>
        /// Creates a test rotation with specified ID
        /// </summary>
        public static Rotation CreateTestRotation(int rotId, int serviceId = 1, string name = "Test Rotation", string abbreviation = "TEST")
        {
            return new Rotation
            {
                RotId = rotId,
                ServiceId = serviceId,
                Name = name,
                Abbreviation = abbreviation
            };
        }
    }
}
