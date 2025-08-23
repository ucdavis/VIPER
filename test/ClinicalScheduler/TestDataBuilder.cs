using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Viper.Classes.SQLContext;

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
    }
}