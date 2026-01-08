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
        private static readonly int TestYear = DateTime.UtcNow.Year;
        private static readonly int TestTermCode = TestYear * 100 + 1;

        private static readonly Service CardiologyService = new()
        {
            ServiceId = 1,
            ServiceName = "Cardiology",
            ShortName = "Cardio"
        };

        private static readonly Service SurgeryService = new()
        {
            ServiceId = 2,
            ServiceName = "Surgery",
            ShortName = "Surg"
        };

        private static readonly Service InternalMedicineService = new()
        {
            ServiceId = 3,
            ServiceName = "Internal Medicine",
            ShortName = "IM"
        };

        public static readonly List<Rotation> TestRotations = new()
        {
            new Rotation
            {
                RotId = 1,
                ServiceId = 1,
                Name = "Cardiology Rotation",
                Abbreviation = "CARD",
                Service = CardiologyService
            },
            new Rotation
            {
                RotId = 2,
                ServiceId = 2,
                Name = "Surgery Rotation",
                Abbreviation = "SURG",
                Service = SurgeryService
            },
            new Rotation
            {
                RotId = 3,
                ServiceId = 3,
                Name = "Internal Medicine Rotation",
                Abbreviation = "IM",
                Service = InternalMedicineService
            }
        };

        public static List<WeekGradYear> TestWeekGradYears => GetTestWeekGradYearsInternal();

        private static List<WeekGradYear> GetTestWeekGradYearsInternal()
        {
            var baseDate = new DateTime(TestYear, 6, 1);
            var prevYearBaseDate = new DateTime(TestYear - 1, 6, 1);

            return new List<WeekGradYear>
            {
                new WeekGradYear
                {
                    WeekId = 1,
                    GradYear = TestYear - 1,
                    Week = new Week
                    {
                        WeekId = 1,
                        DateStart = prevYearBaseDate,
                        DateEnd = prevYearBaseDate.AddDays(6),
                        TermCode = (TestYear - 1) * 100 + 1
                    }
                },
                new WeekGradYear
                {
                    WeekId = 2,
                    GradYear = TestYear - 1,
                    Week = new Week
                    {
                        WeekId = 2,
                        DateStart = prevYearBaseDate.AddDays(7),
                        DateEnd = prevYearBaseDate.AddDays(13),
                        TermCode = (TestYear - 1) * 100 + 1
                    }
                },
                new WeekGradYear
                {
                    WeekId = 1,
                    GradYear = TestYear,
                    Week = new Week
                    {
                        WeekId = 1,
                        DateStart = baseDate,
                        DateEnd = baseDate.AddDays(6),
                        TermCode = TestTermCode
                    }
                },
                new WeekGradYear
                {
                    WeekId = 2,
                    GradYear = TestYear,
                    Week = new Week
                    {
                        WeekId = 2,
                        DateStart = baseDate.AddDays(7),
                        DateEnd = baseDate.AddDays(13),
                        TermCode = TestTermCode
                    }
                }
            };
        }

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
