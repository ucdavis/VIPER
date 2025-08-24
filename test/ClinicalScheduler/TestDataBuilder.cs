using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Static factory methods for creating test data and common test scenarios
    /// </summary>
    public static class TestDataBuilder
    {
        /// <summary>
        /// Creates a service provider for dependency injection in controller tests
        /// </summary>
        public static IServiceProvider CreateTestServiceProvider(ClinicalSchedulerContext context)
        {
            return new ServiceCollection()
                .AddSingleton(context)
                .AddSingleton<RAPSContext>(new Mock<RAPSContext>().Object)
                .BuildServiceProvider();
        }

        /// <summary>
        /// Sets up HTTP context for a controller with dependency injection
        /// </summary>
        public static void SetupControllerContext(ControllerBase controller, IServiceProvider serviceProvider)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProvider
                }
            };
        }

        /// <summary>
        /// Creates a dictionary of service permissions for testing
        /// </summary>
        public static Dictionary<int, bool> CreateServicePermissions(params (int serviceId, bool canEdit)[] permissions)
        {
            var result = new Dictionary<int, bool>();
            foreach (var (serviceId, canEdit) in permissions)
            {
                result[serviceId] = canEdit;
            }
            return result;
        }

        /// <summary>
        /// Common test scenarios for service permissions
        /// </summary>
        public static class ServicePermissionScenarios
        {
            /// <summary>
            /// User has permission only for Cardiology service
            /// </summary>
            public static Dictionary<int, bool> CardiologyOnly => CreateServicePermissions(
                (ClinicalSchedulerTestBase.CardiologyServiceId, true),
                (ClinicalSchedulerTestBase.SurgeryServiceId, false),
                (ClinicalSchedulerTestBase.InternalMedicineServiceId, false),
                (ClinicalSchedulerTestBase.EmergencyMedicineServiceId, false)
            );

            /// <summary>
            /// User has manage permission - can edit all services
            /// </summary>
            public static Dictionary<int, bool> ManagePermission => CreateServicePermissions(
                (ClinicalSchedulerTestBase.CardiologyServiceId, true),
                (ClinicalSchedulerTestBase.SurgeryServiceId, true),
                (ClinicalSchedulerTestBase.InternalMedicineServiceId, true),
                (ClinicalSchedulerTestBase.EmergencyMedicineServiceId, true)
            );

            /// <summary>
            /// User has no permissions for any service
            /// </summary>
            public static Dictionary<int, bool> NoPermissions => CreateServicePermissions(
                (ClinicalSchedulerTestBase.CardiologyServiceId, false),
                (ClinicalSchedulerTestBase.SurgeryServiceId, false),
                (ClinicalSchedulerTestBase.InternalMedicineServiceId, false),
                (ClinicalSchedulerTestBase.EmergencyMedicineServiceId, false)
            );
        }

        /// <summary>
        /// Creates a test user for testing purposes
        /// </summary>
        public static AaudUser CreateUser(string mothraId, string loginId = null, string displayName = null)
        {
            return new AaudUser
            {
                ClientId = "VIPER",
                MothraId = mothraId,
                LoginId = loginId ?? mothraId,
                LastName = mothraId,
                FirstName = "Test",
                DisplayLastName = mothraId,
                DisplayFirstName = "Test",
                DisplayFullName = displayName ?? $"Test {mothraId}"
            };
        }

        /// <summary>
        /// Creates a test instructor schedule
        /// </summary>
        public static InstructorSchedule CreateInstructorSchedule(string mothraId, int rotationId, int weekId, bool isEvaluator = false, string role = null)
        {
            return new InstructorSchedule
            {
                MothraId = mothraId,
                RotationId = rotationId,
                WeekId = weekId,
                Evaluator = isEvaluator,
                Role = role
            };
        }

        /// <summary>
        /// Creates a test service
        /// </summary>
        public static Service CreateService(int serviceId, string serviceName, string shortName = null, string scheduleEditPermission = null)
        {
            if (string.IsNullOrEmpty(serviceName))
                throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

            return new Service
            {
                ServiceId = serviceId,
                ServiceName = serviceName,
                ShortName = shortName ?? (serviceName.Length > 5 ? serviceName.Substring(0, 5) : serviceName),
                ScheduleEditPermission = scheduleEditPermission
            };
        }

        /// <summary>
        /// Creates a test rotation
        /// </summary>
        public static Rotation CreateRotation(int rotId, string name, int serviceId, string abbreviation = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Rotation name cannot be null or empty", nameof(name));

            return new Rotation
            {
                RotId = rotId,
                Name = name,
                ServiceId = serviceId,
                Abbreviation = abbreviation ?? (name.Length > 8 ? name.Substring(0, 8) : name)
            };
        }
    }
}