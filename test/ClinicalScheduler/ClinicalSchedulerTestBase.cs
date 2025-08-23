using Microsoft.EntityFrameworkCore;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Viper.Models.AAUD;

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

        // Permission constants
        public const string CardiologyEditPermission = "SVMSecure.ClnSched.Edit.Cardio";
        public const string InternalMedicineEditPermission = "SVMSecure.ClnSched.Edit.IM";

        // Shared test infrastructure
        protected readonly ClinicalSchedulerContext Context;
        protected readonly Mock<RAPSContext> MockRapsContext;
        protected readonly Mock<IUserHelper> MockUserHelper;

        protected ClinicalSchedulerTestBase()
        {
            Context = CreateInMemoryContext();
            MockRapsContext = new Mock<RAPSContext>();
            MockUserHelper = new Mock<IUserHelper>();
            SeedCommonTestData();
        }

        /// <summary>
        /// Creates an in-memory database context for testing
        /// </summary>
        private static ClinicalSchedulerContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ClinicalSchedulerContext(options);
        }

        /// <summary>
        /// Seeds common test data used across multiple tests
        /// </summary>
        private void SeedCommonTestData()
        {
            // Add test services
            var services = new[]
            {
                CreateCardiologyService(),
                CreateSurgeryService(),
                CreateInternalMedicineService(),
                CreateEmergencyMedicineService()
            };

            Context.Services.AddRange(services);

            // Add test rotations
            var rotations = new[]
            {
                CreateCardiologyRotation(),
                CreateSurgeryRotation()
            };

            Context.Rotations.AddRange(rotations);
            Context.SaveChanges();
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

        private static Rotation CreateCardiologyRotation() => new()
        {
            RotId = CardiologyRotationId,
            ServiceId = CardiologyServiceId,
            Name = "Cardiology Rotation",
            Abbreviation = "CARD"
        };

        private static Rotation CreateSurgeryRotation() => new()
        {
            RotId = SurgeryRotationId,
            ServiceId = SurgeryServiceId,
            Name = "Surgery Rotation",
            Abbreviation = "SURG"
        };

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Context?.Dispose();
            }
        }
    }
}