using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Viper.Models.AAUD;
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

        // Shared test infrastructure
        protected readonly Mock<ClinicalSchedulerContext> MockContext;
        protected readonly ClinicalSchedulerContext Context; // For compatibility with existing tests
        protected readonly Mock<RAPSContext> MockRapsContext;
        protected readonly Mock<IUserHelper> MockUserHelper;

        protected ClinicalSchedulerTestBase()
        {
            // Create real DbContextOptions for in-memory database (required for mock to work)
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            MockContext = new Mock<ClinicalSchedulerContext>(options);
            Context = MockContext.Object; // Provide the mock object for compatibility
            MockRapsContext = new Mock<RAPSContext>();
            MockUserHelper = new Mock<IUserHelper>();
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

        /// <summary>
        /// Sets up weeks mock DbSet
        /// </summary>
        private void SetupWeeks()
        {
            var weeks = new List<Week>
            {
                new Week
                {
                    WeekId = 1,
                    DateStart = DateTime.UtcNow.AddDays(-7),
                    DateEnd = DateTime.UtcNow.AddDays(-1),
                    TermCode = 202501
                },
                new Week
                {
                    WeekId = 2,
                    DateStart = DateTime.UtcNow.AddDays(0),
                    DateEnd = DateTime.UtcNow.AddDays(6),
                    TermCode = 202501
                },
                new Week
                {
                    WeekId = 10,
                    DateStart = DateTime.UtcNow.AddDays(63),
                    DateEnd = DateTime.UtcNow.AddDays(69),
                    TermCode = 202501
                },
                new Week
                {
                    WeekId = 15,
                    DateStart = DateTime.UtcNow.AddDays(98),
                    DateEnd = DateTime.UtcNow.AddDays(104),
                    TermCode = 202501
                },
                new Week
                {
                    WeekId = 20,
                    DateStart = DateTime.UtcNow.AddDays(133),
                    DateEnd = DateTime.UtcNow.AddDays(139),
                    TermCode = 202501
                }
            };

            var mockDbSet = weeks.AsQueryable().BuildMockDbSet();
            MockContext.Setup(c => c.Weeks).Returns(mockDbSet.Object);
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

            var mockDbSet = instructorSchedules.AsQueryable().BuildMockDbSet();
            MockContext.Setup(c => c.InstructorSchedules).Returns(mockDbSet.Object);
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

            var mockDbSet = services.AsQueryable().BuildMockDbSet();
            MockContext.Setup(c => c.Services).Returns(mockDbSet.Object);
        }

        /// <summary>
        /// Sets up schedule audits mock DbSet
        /// </summary>
        private void SetupScheduleAudits()
        {
            var scheduleAudits = new List<ScheduleAudit>();

            var mockDbSet = scheduleAudits.AsQueryable().BuildMockDbSet();
            MockContext.Setup(c => c.ScheduleAudits).Returns(mockDbSet.Object);
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
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);
            MockUserHelper.Setup(x => x.HasPermission(MockRapsContext.Object, testUser, ClinicalSchedulePermissions.Manage))
                .Returns(hasPermission);
        }

        /// <summary>
        /// Sets up UserHelper mock to return a user without manage permission but with specific service permissions
        /// </summary>
        protected void SetupUserWithoutManagePermission(string userMothraId = TestUserMothraId)
        {
            var testUser = CreateTestUser();
            testUser.MothraId = userMothraId;
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            // User doesn't have manage permission
            MockUserHelper.Setup(x => x.HasPermission(MockRapsContext.Object, testUser, ClinicalSchedulePermissions.Manage))
                .Returns(false);

            // User has permission for Cardiology only
            MockUserHelper.Setup(x => x.HasPermission(MockRapsContext.Object, testUser, CardiologyEditPermission))
                .Returns(true);

            // Default to false for any other permissions
            MockUserHelper.Setup(x => x.HasPermission(MockRapsContext.Object, testUser, It.Is<string>(p => p != CardiologyEditPermission && p != ClinicalSchedulePermissions.Manage)))
                .Returns(false);
        }

        /// <summary>
        /// Sets up UserHelper mock to return null user
        /// </summary>
        protected void SetupNullUser()
        {
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns((AaudUser?)null);
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
                MockUserHelper.Setup(x => x.GetCurrentUser()).Returns((AaudUser?)null);
                return;
            }

            var testUser = CreateTestUser();
            testUser.MothraId = userMothraId;
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

            // Default all permissions to false
            MockUserHelper.Setup(x => x.HasPermission(It.IsAny<RAPSContext>(), It.IsAny<AaudUser>(), It.IsAny<string>()))
                .Returns(false);

            // Set up the specific permissions to return true
            foreach (var permission in permissions)
            {
                MockUserHelper.Setup(x => x.HasPermission(MockRapsContext.Object, testUser, permission))
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
