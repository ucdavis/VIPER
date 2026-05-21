using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;
using NSubstitute;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.ClinicalScheduler;
using Viper.test.ClinicalScheduler.Setup;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Base class for Clinical Scheduler tests providing common test infrastructure
    /// </summary>
    public abstract class ClinicalSchedulerTestBase : IDisposable
    {
        // Test user constants
        public const string TestUserMothraId = "testuser";
        public const string TestUserLoginId = "testuser";
        public const string TestUserDisplayName = "Test User";

        // Service ID constants
        public const int CardiologyServiceId = 1;
        public const int SurgeryServiceId = 2;
        public const int InternalMedicineServiceId = 3;
        public const int EmergencyMedicineServiceId = 4;

        // Rotation ID constants
        public const int CardiologyRotationId = 1;
        public const int SurgeryRotationId = 2;

        // Instructor Schedule ID constants
        public const int TestInstructorScheduleId = 1; // Schedule belonging to TestUserMothraId
        public const int OtherInstructorScheduleId = 2; // Schedule belonging to different user

        // Permission constants
        public const string CardiologyEditPermission = "SVMSecure.ClnSched.Edit.Cardio";
        public const string InternalMedicineEditPermission = "SVMSecure.ClnSched.Edit.IM";

        public static readonly int TestYear = DateTime.UtcNow.Year;
        public static readonly int TestTermCode = TestYear * 100 + 1;

        // Shared test infrastructure
        protected readonly ClinicalSchedulerContext MockContext;
        protected readonly ClinicalSchedulerContext Context; // For compatibility with existing tests
        protected readonly RAPSContext MockRapsContext;
        protected readonly IUserHelper MockUserHelper;

        protected ClinicalSchedulerTestBase()
        {
            // Create real DbContextOptions for in-memory database (required for mock to work)
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            MockContext = Substitute.For<ClinicalSchedulerContext>(options);
            Context = MockContext; // Provide the substitute directly for compatibility
            MockRapsContext = Substitute.For<RAPSContext>();
            MockUserHelper = Substitute.For<IUserHelper>();
            SetupMockDbSets();
        }

        /// <summary>
        /// Sets up mock DbSets for testing
        /// </summary>
        private void SetupMockDbSets()
        {
            // Setup mock DbSets with test data
            SetupClinicalSchedulerPeople.SetupPersonsTable(MockContext);
            SetupClinicalSchedulerRotations.SetupRotationsTable(MockContext);
            SetupClinicalSchedulerRotations.SetupWeekGradYearsTable(MockContext);
            SetupWeeks();
            SetupInstructorSchedules();
            SetupServices();
            SetupScheduleAudits();
        }

        private void SetupWeeks()
        {
            var baseDate = new DateTime(TestYear, 6, 1, 0, 0, 0, DateTimeKind.Local);

            var weeks = new List<Week>
            {
                new Week
                {
                    WeekId = 1,
                    DateStart = baseDate,
                    DateEnd = baseDate.AddDays(6),
                    TermCode = TestTermCode
                },
                new Week
                {
                    WeekId = 2,
                    DateStart = baseDate.AddDays(7),
                    DateEnd = baseDate.AddDays(13),
                    TermCode = TestTermCode
                },
                new Week
                {
                    WeekId = 10,
                    DateStart = baseDate.AddDays(63),
                    DateEnd = baseDate.AddDays(69),
                    TermCode = TestTermCode
                },
                new Week
                {
                    WeekId = 15,
                    DateStart = baseDate.AddDays(98),
                    DateEnd = baseDate.AddDays(104),
                    TermCode = TestTermCode
                },
                new Week
                {
                    WeekId = 20,
                    DateStart = baseDate.AddDays(133),
                    DateEnd = baseDate.AddDays(139),
                    TermCode = TestTermCode
                }
            };

            var mockDbSet = weeks.BuildMockDbSet();
            MockContext.Weeks.Returns(mockDbSet);
        }

        /// <summary>
        /// Sets up instructor schedules mock DbSet
        /// </summary>
        private void SetupInstructorSchedules()
        {
            var instructorSchedules = new List<InstructorSchedule>
            {
                new InstructorSchedule
                {
                    InstructorScheduleId = TestInstructorScheduleId,
                    MothraId = TestUserMothraId, // Belongs to test user
                    RotationId = CardiologyRotationId,
                    WeekId = 1,
                    Evaluator = false
                },
                new InstructorSchedule
                {
                    InstructorScheduleId = OtherInstructorScheduleId,
                    MothraId = "otheruser", // Belongs to different user
                    RotationId = SurgeryRotationId,
                    WeekId = 1,
                    Evaluator = false
                }
            };

            var mockDbSet = instructorSchedules.BuildMockDbSet();
            MockContext.InstructorSchedules.Returns(mockDbSet);
        }

        /// <summary>
        /// Sets up services mock DbSet
        /// </summary>
        private void SetupServices()
        {
            var services = new List<Service>
            {
                CreateCardiologyService(),
                CreateSurgeryService(),
                CreateInternalMedicineService(),
                CreateEmergencyMedicineService()
            };

            var mockDbSet = services.BuildMockDbSet();
            MockContext.Services.Returns(mockDbSet);
        }

        /// <summary>
        /// Sets up schedule audits mock DbSet
        /// </summary>
        private void SetupScheduleAudits()
        {
            var scheduleAudits = new List<ScheduleAudit>();

            var mockDbSet = scheduleAudits.BuildMockDbSet();
            MockContext.ScheduleAudits.Returns(mockDbSet);
        }

        /// <summary>
        /// Creates a test user with standard properties
        /// </summary>
        protected static AaudUser CreateTestUser() => new()
        {
            MothraId = TestUserMothraId,
            LoginId = TestUserLoginId,
            DisplayFullName = TestUserDisplayName
        };

        /// <summary>
        /// Sets up UserHelper mock to return a user with manage permission
        /// </summary>
        protected void SetupUserWithManagePermission(string userMothraId = TestUserMothraId, bool hasPermission = true)
        {
            var testUser = CreateTestUser();
            testUser.MothraId = userMothraId;
            MockUserHelper.GetCurrentUser().Returns(testUser);
            MockUserHelper.HasPermission(MockRapsContext, testUser, ClinicalSchedulePermissions.Manage)
                .Returns(hasPermission);
        }

        /// <summary>
        /// Sets up UserHelper mock to return a user without manage permission but with specific service permissions
        /// </summary>
        protected void SetupUserWithoutManagePermission(string userMothraId = TestUserMothraId)
        {
            var testUser = CreateTestUser();
            testUser.MothraId = userMothraId;
            MockUserHelper.GetCurrentUser().Returns(testUser);

            // User doesn't have manage permission
            MockUserHelper.HasPermission(MockRapsContext, testUser, ClinicalSchedulePermissions.Manage)
                .Returns(false);

            // User has permission for Cardiology only
            MockUserHelper.HasPermission(MockRapsContext, testUser, CardiologyEditPermission)
                .Returns(true);

            // Default to false for any other permissions
            MockUserHelper.HasPermission(MockRapsContext, testUser, Arg.Is<string>(p => p != CardiologyEditPermission && p != ClinicalSchedulePermissions.Manage))
                .Returns(false);
        }

        /// <summary>
        /// Sets up UserHelper mock to return null user
        /// </summary>
        protected void SetupNullUser()
        {
            MockUserHelper.GetCurrentUser().Returns((AaudUser?)null);
        }

        /// <summary>
        /// Sets up the UserHelper mock to return a user with a specific set of permissions.
        /// </summary>
        /// <param name="userMothraId">The MothraId of the user to set up. If null, sets up a null user.</param>
        /// <param name="permissions">The list of permissions the user should have.</param>
        protected void SetupUserWithPermissions(string? userMothraId, IEnumerable<string> permissions)
        {
            if (userMothraId == null)
            {
                MockUserHelper.GetCurrentUser().Returns((AaudUser?)null);
                return;
            }

            var testUser = CreateTestUser();
            testUser.MothraId = userMothraId;
            MockUserHelper.GetCurrentUser().Returns(testUser);

            // Default all permissions to false
            MockUserHelper.HasPermission(Arg.Any<RAPSContext>(), Arg.Any<AaudUser>(), Arg.Any<string>())
                .Returns(false);

            // Set up the specific permissions to return true
            foreach (var permission in permissions)
            {
                MockUserHelper.HasPermission(MockRapsContext, testUser, permission)
                    .Returns(true);
            }
        }

        // Test data creation methods
        private static Service CreateCardiologyService() => new()
        {
            ServiceId = CardiologyServiceId,
            ServiceName = "Cardiology",
            ShortName = "Cardio",
            ScheduleEditPermission = CardiologyEditPermission
        };

        private static Service CreateSurgeryService() => new()
        {
            ServiceId = SurgeryServiceId,
            ServiceName = "Surgery",
            ShortName = "Surg",
            ScheduleEditPermission = null // Uses default permission
        };

        private static Service CreateInternalMedicineService() => new()
        {
            ServiceId = InternalMedicineServiceId,
            ServiceName = "Internal Medicine",
            ShortName = "IM",
            ScheduleEditPermission = InternalMedicineEditPermission
        };

        private static Service CreateEmergencyMedicineService() => new()
        {
            ServiceId = EmergencyMedicineServiceId,
            ServiceName = "Emergency Medicine",
            ShortName = "ER",
            ScheduleEditPermission = "" // Empty string, uses default
        };

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // No resources to dispose when using mocks
        }
    }
}
