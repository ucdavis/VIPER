using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
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
        public static AaudUser CreateUser(string mothraId, string? loginId = null, string? displayName = null)
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
        public static InstructorSchedule CreateInstructorSchedule(string mothraId, int rotationId, int weekId, bool isEvaluator = false, string? role = null)
        {
            return new InstructorSchedule
            {
                // Don't set InstructorScheduleId - let Entity Framework assign it
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
        public static Service CreateService(int serviceId, string serviceName, string? shortName = null, string? scheduleEditPermission = null)
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
        public static Rotation CreateRotation(int rotId, string name, int serviceId, string? abbreviation = null)
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

        /// <summary>
        /// Creates a test Person entity for Clinical Scheduler context
        /// </summary>
        public static Viper.Models.ClinicalScheduler.Person CreatePerson(string mothraId, string firstName = "Test", string lastName = "User")
        {
            return new Viper.Models.ClinicalScheduler.Person
            {
                IdsMothraId = mothraId,
                PersonDisplayFullName = $"{lastName}, {firstName}",
                PersonDisplayLastName = lastName,
                PersonDisplayFirstName = firstName
            };
        }

        /// <summary>
        /// Creates a test Week entity
        /// </summary>
        public static Week CreateWeek(int weekId, DateTime? startDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7 * (10 - weekId));
            return new Week
            {
                WeekId = weekId,
                DateStart = start,
                DateEnd = start.AddDays(6),
                TermCode = (DateTime.Now.Year + 1) * 100 + 1
            };
        }

        /// <summary>
        /// Creates a test WeekGradYear entity
        /// </summary>
        public static WeekGradYear CreateWeekGradYear(int weekId, int gradYear)
        {
            return new WeekGradYear
            {
                WeekId = weekId,
                GradYear = gradYear
            };
        }

        /// <summary>
        /// Creates a complete week scenario with both Week and WeekGradYear entities
        /// </summary>
        public static (Week week, WeekGradYear weekGradYear) CreateWeekScenario(int weekId, int gradYear, DateTime? startDate = null)
        {
            var week = CreateWeek(weekId, startDate);
            var weekGradYear = CreateWeekGradYear(weekId, gradYear);
            weekGradYear.Week = week;
            return (week, weekGradYear);
        }

        /// <summary>
        /// Integration test helpers
        /// </summary>
        public static class IntegrationHelpers
        {
            /// <summary>
            /// Creates and configures an in-memory AAUD context with test data
            /// </summary>
            public static AAUDContext CreateAAUDContext()
            {
                var options = new DbContextOptionsBuilder<AAUDContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;
                var context = new AAUDContext(options);

                // Seed with standard test users
                var testUsers = new[]
                {
                    new AaudUser
                    {
                        MothraId = ClinicalSchedulerTestBase.TestUserMothraId,
                        LoginId = ClinicalSchedulerTestBase.TestUserLoginId,
                        ClientId = "ucd.edu",
                        AaudUserId = 999,
                        DisplayFirstName = "Test",
                        DisplayLastName = "User",
                        DisplayFullName = ClinicalSchedulerTestBase.TestUserDisplayName,
                        FirstName = "Test",
                        LastName = "User"
                    },
                    CreateUser("clinician1", "clinician1", "Alice Johnson"),
                    CreateUser("clinician2", "clinician2", "Bob Smith"),
                    CreateUser("12345", "jdoe", "John Doe"),
                    CreateUser("67890", "jsmith", "Jane Smith")
                };

                context.Set<AaudUser>().AddRange(testUsers);
                context.SaveChanges();
                return context;
            }

            /// <summary>
            /// Creates and configures an in-memory RAPS context with test permissions
            /// </summary>
            public static RAPSContext CreateRAPSContext()
            {
                var options = new DbContextOptionsBuilder<RAPSContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;
                var context = new RAPSContext(options);

                // Setup HttpHelper.Cache for UserHelper permission caching
                var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
                Viper.HttpHelper.Configure(memoryCache, null!, null!, null!, null!, null!);

                // Create standard test permissions
                var permissions = new List<Viper.Models.RAPS.TblPermission>
                {
                    new() { PermissionId = 1, Permission = ClinicalSchedulePermissions.Base, Description = "Clinical Scheduler Base Permission" },
                    new() { PermissionId = 2, Permission = ClinicalSchedulePermissions.Manage, Description = "Clinical Scheduler Manage Permission" },
                    new() { PermissionId = 3, Permission = ClinicalSchedulerTestBase.CardiologyEditPermission, Description = "Cardiology Edit Permission" },
                    new() { PermissionId = 4, Permission = ClinicalSchedulePermissions.EditOwnSchedule, Description = "Edit Own Schedule Permission" },
                    new() { PermissionId = 5, Permission = ClinicalSchedulePermissions.EditClnSchedules, Description = "Edit Clinical Schedules Permission" },
                    new() { PermissionId = 6, Permission = ClinicalSchedulePermissions.Admin, Description = "Clinical Scheduler Admin Permission" }
                };

                context.TblPermissions.AddRange(permissions);
                context.SaveChanges();
                return context;
            }

            /// <summary>
            /// Adds standard member permissions for a user to RAPS context
            /// </summary>
            public static void AddMemberPermissions(RAPSContext rapsContext, string mothraId, params string[] permissions)
            {
                var memberPermissions = new List<Viper.Models.RAPS.TblMemberPermission>();

                foreach (var permission in permissions)
                {
                    var permissionEntity = rapsContext.TblPermissions.FirstOrDefault(p => p.Permission == permission);
                    if (permissionEntity != null)
                    {
                        memberPermissions.Add(new Viper.Models.RAPS.TblMemberPermission
                        {
                            MemberId = mothraId,
                            PermissionId = permissionEntity.PermissionId,
                            Access = 1,
                            StartDate = DateTime.Today.AddYears(-1),
                            EndDate = null
                        });
                    }
                }

                rapsContext.TblMemberPermissions.AddRange(memberPermissions);
                rapsContext.SaveChanges();
            }

            /// <summary>
            /// Creates a set of standard test clinicians in both AAUD and Clinical Scheduler contexts
            /// </summary>
            public static void SeedCliniciansData(ClinicalSchedulerContext clinicalContext, AAUDContext aaudContext)
            {
                // Add clinicians to Clinical Scheduler context
                var persons = new[]
                {
                    CreatePerson("12345", "John", "Doe"),
                    CreatePerson("67890", "Jane", "Smith")
                };
                clinicalContext.Persons.AddRange(persons);

                // Add corresponding AAUD data
                var aaudUsers = new[]
                {
                    new AaudUser
                    {
                        ClientId = "test-client",
                        MothraId = "12345",
                        FirstName = "John",
                        LastName = "Doe",
                        DisplayFirstName = "John",
                        DisplayLastName = "Doe",
                        DisplayFullName = "John Doe",
                        LoginId = "jdoe",
                        EmployeeId = "E12345",
                        CurrentEmployee = true,
                        Current = 1
                    },
                    new AaudUser
                    {
                        ClientId = "test-client",
                        MothraId = "67890",
                        FirstName = "Jane",
                        LastName = "Smith",
                        DisplayFirstName = "Jane",
                        DisplayLastName = "Smith",
                        DisplayFullName = "Jane Smith",
                        LoginId = "jsmith",
                        EmployeeId = "E67890",
                        CurrentEmployee = true,
                        Current = 1
                    }
                };
                aaudContext.AaudUsers.AddRange(aaudUsers);
            }

            /// <summary>
            /// Creates a complete schedule setup scenario with weeks, rotations, and schedules
            /// </summary>
            /// <param name="context">The Clinical Scheduler context to seed</param>
            /// <param name="baseYear">Base year for test data. If not provided, uses current year for convenience.</param>
            public static void CreateCompleteScheduleScenario(ClinicalSchedulerContext context, int? baseYear = null)
            {
                var currentYear = baseYear ?? DateTime.Now.Year;

                // Create weeks and grad years
                var weeks = new[]
                {
                    CreateWeek(10, new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                    CreateWeek(11, new DateTime(currentYear, 1, 8, 0, 0, 0, DateTimeKind.Utc))
                };
                context.Weeks.AddRange(weeks);

                var weekGradYears = new[]
                {
                    CreateWeekGradYear(10, currentYear + 3),
                    CreateWeekGradYear(11, currentYear + 3)
                };
                context.WeekGradYears.AddRange(weekGradYears);

                // Create instructor schedules
                var instructorSchedules = new[]
                {
                    new InstructorSchedule
                    {
                        InstructorScheduleId = 100,
                        MothraId = "12345",
                        RotationId = ClinicalSchedulerTestBase.CardiologyRotationId,
                        WeekId = 10,
                        Evaluator = false
                    },
                    new InstructorSchedule
                    {
                        InstructorScheduleId = 101,
                        MothraId = "67890",
                        RotationId = ClinicalSchedulerTestBase.SurgeryRotationId,
                        WeekId = 11,
                        Evaluator = true
                    }
                };
                context.InstructorSchedules.AddRange(instructorSchedules);
            }
        }
    }
}
