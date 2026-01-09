using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Effort;

/// <summary>
/// Base class for Effort integration tests that require AAUD and RAPS contexts.
/// Provides pre-configured contexts and common setup methods to eliminate
/// repetitive test data setup across integration test files.
/// </summary>
public abstract class EffortIntegrationTestBase : IDisposable
{
    // Test user constants
    public const string TestUserMothraId = "testuser";
    public const string TestUserLoginId = "testuser";
    public const string TestUserDisplayName = "Test User";
    public const int TestUserAaudId = 999;

    // Department constants
    public const string DvmDepartment = "DVM";
    public const string VmeDepartment = "VME";
    public const string ApcDepartment = "APC";

    // Term constants
    public const int TestTermCode = 202410;

    protected readonly EffortDbContext EffortContext;
    protected readonly RAPSContext RapsContext;
    protected readonly Mock<IUserHelper> MockUserHelper;

    protected EffortIntegrationTestBase()
    {
        // Create real in-memory database for Entity Framework operations
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        EffortContext = new EffortDbContext(effortOptions);

        // Create RAPS context for permission checking
        RapsContext = CreateRAPSContext();

        // Setup UserHelper mock
        MockUserHelper = new Mock<IUserHelper>();

        // Seed basic test data
        SeedBasicTestData();
    }

    /// <summary>
    /// Creates and configures an in-memory RAPS context with Effort permissions
    /// </summary>
    private static RAPSContext CreateRAPSContext()
    {
        var options = new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new RAPSContext(options);

        // Setup HttpHelper.Cache for UserHelper permission caching
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        Viper.HttpHelper.Configure(memoryCache, null!, null!, null!, null!, null!);

        // Create standard Effort permissions
        var permissions = new List<Viper.Models.RAPS.TblPermission>
        {
            new() { PermissionId = 1, Permission = EffortPermissions.Base, Description = "Effort Base Permission" },
            new() { PermissionId = 2, Permission = EffortPermissions.ViewAllDepartments, Description = "View All Departments" },
            new() { PermissionId = 3, Permission = EffortPermissions.ViewDept, Description = "View Department" },
            new() { PermissionId = 4, Permission = EffortPermissions.EditCourse, Description = "Edit Course" },
            new() { PermissionId = 5, Permission = EffortPermissions.ImportCourse, Description = "Import Course" },
            new() { PermissionId = 6, Permission = EffortPermissions.DeleteCourse, Description = "Delete Course" },
            new() { PermissionId = 7, Permission = EffortPermissions.EditEffort, Description = "Edit Effort" },
            new() { PermissionId = 8, Permission = EffortPermissions.CreateEffort, Description = "Create Effort" },
            new() { PermissionId = 9, Permission = EffortPermissions.DeleteEffort, Description = "Delete Effort" },
            new() { PermissionId = 10, Permission = EffortPermissions.VerifyEffort, Description = "Verify Effort" },
            new() { PermissionId = 11, Permission = EffortPermissions.ManageRCourseEnrollment, Description = "Manage R Course Enrollment" },
            new() { PermissionId = 12, Permission = EffortPermissions.ViewAudit, Description = "View Audit" },
            new() { PermissionId = 13, Permission = EffortPermissions.ManageTerms, Description = "Manage Terms" },
            new() { PermissionId = 14, Permission = EffortPermissions.ManageUnits, Description = "Manage Units" },
            new() { PermissionId = 15, Permission = EffortPermissions.ManageSessionTypes, Description = "Manage Session Types" }
        };

        context.TblPermissions.AddRange(permissions);
        context.SaveChanges();
        return context;
    }

    /// <summary>
    /// Seeds basic test data required for integration tests
    /// </summary>
    private void SeedBasicTestData()
    {
        // Add test term
        EffortContext.Terms.Add(new EffortTerm
        {
            TermCode = TestTermCode,
            Status = "Open",
            CreatedDate = DateTime.UtcNow
        });

        // Add test courses
        var courses = new[]
        {
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = DvmDepartment },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 15, Units = 3, CustDept = VmeDepartment },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 10, Units = 2, CustDept = ApcDepartment }
        };
        EffortContext.Courses.AddRange(courses);

        // Add test persons
        var persons = new[]
        {
            new EffortPerson { PersonId = TestUserAaudId, TermCode = TestTermCode, FirstName = "Test", LastName = "User", EffortDept = DvmDepartment },
            new EffortPerson { PersonId = 1001, TermCode = TestTermCode, FirstName = "Alice", LastName = "Johnson", EffortDept = VmeDepartment },
            new EffortPerson { PersonId = 1002, TermCode = TestTermCode, FirstName = "Bob", LastName = "Smith", EffortDept = ApcDepartment }
        };
        EffortContext.Persons.AddRange(persons);

        EffortContext.SaveChanges();
    }

    /// <summary>
    /// Creates a test user with standard properties
    /// </summary>
    protected static AaudUser CreateTestUser(string? mothraId = null, int? aaudUserId = null) => new()
    {
        MothraId = mothraId ?? TestUserMothraId,
        LoginId = mothraId ?? TestUserLoginId,
        DisplayFullName = TestUserDisplayName,
        AaudUserId = aaudUserId ?? TestUserAaudId
    };

    /// <summary>
    /// Sets up user with ViewAllDepartments (full access) permission
    /// </summary>
    protected void SetupUserWithFullAccess()
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.ViewAllDepartments);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.ViewAllDepartments
        });
    }

    /// <summary>
    /// Sets up user with department-level view permission
    /// </summary>
    protected void SetupUserWithDepartmentViewAccess(params string[] departments)
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.ViewDept);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.ViewDept
        });

        // Add UserAccess entries for departments
        foreach (var dept in departments)
        {
            EffortContext.UserAccess.Add(new UserAccess
            {
                PersonId = TestUserAaudId,
                DepartmentCode = dept,
                IsActive = true
            });
        }
        EffortContext.SaveChanges();
    }

    /// <summary>
    /// Sets up user with course edit permission
    /// </summary>
    protected void SetupUserWithEditCoursePermission()
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.EditCourse);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.EditCourse
        });
    }

    /// <summary>
    /// Sets up user with course import permission
    /// </summary>
    protected void SetupUserWithImportCoursePermission()
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.ImportCourse);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.ImportCourse
        });
    }

    /// <summary>
    /// Sets up user with course delete permission
    /// </summary>
    protected void SetupUserWithDeleteCoursePermission()
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.DeleteCourse);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.DeleteCourse
        });
    }

    /// <summary>
    /// Sets up user with effort edit permission for a department
    /// </summary>
    protected void SetupUserWithEditEffortPermission(params string[] departments)
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.ViewDept,
            EffortPermissions.EditEffort);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.ViewDept,
            EffortPermissions.EditEffort
        });

        // Add UserAccess entries for departments
        foreach (var dept in departments)
        {
            EffortContext.UserAccess.Add(new UserAccess
            {
                PersonId = TestUserAaudId,
                DepartmentCode = dept,
                IsActive = true
            });
        }
        EffortContext.SaveChanges();
    }

    /// <summary>
    /// Sets up user with self-service (VerifyEffort) permission only
    /// </summary>
    protected void SetupUserWithSelfServiceAccess()
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.VerifyEffort);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.VerifyEffort
        });
    }

    /// <summary>
    /// Sets up user with base permission only (view-only, no edit)
    /// </summary>
    protected void SetupUserWithBasePermissionOnly()
    {
        AddMemberPermissions(TestUserMothraId, EffortPermissions.Base);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base
        });
    }

    /// <summary>
    /// Sets up user with ManageUnits permission
    /// </summary>
    protected void SetupUserWithManageUnitsPermission()
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.ManageUnits);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.ManageUnits
        });
    }

    /// <summary>
    /// Sets up user with ManageSessionTypes permission
    /// </summary>
    protected void SetupUserWithManageSessionTypesPermission()
    {
        AddMemberPermissions(TestUserMothraId,
            EffortPermissions.Base,
            EffortPermissions.ManageSessionTypes);

        SetupUserWithPermissionsForIntegration(TestUserMothraId, new[]
        {
            EffortPermissions.Base,
            EffortPermissions.ManageSessionTypes
        });
    }

    /// <summary>
    /// Sets up user with no permissions
    /// </summary>
    protected void SetupUserWithNoPermissions()
    {
        var testUser = CreateTestUser();
        MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);
        MockUserHelper.Setup(x => x.HasPermission(It.IsAny<RAPSContext>(), It.IsAny<AaudUser>(), It.IsAny<string>()))
            .Returns(false);
    }

    /// <summary>
    /// Sets up null user (unauthenticated)
    /// </summary>
    protected void SetupNullUser()
    {
        MockUserHelper.Setup(x => x.GetCurrentUser()).Returns((AaudUser?)null);
    }

    /// <summary>
    /// Sets up user permissions for integration tests
    /// </summary>
    protected void SetupUserWithPermissionsForIntegration(string? userMothraId, IEnumerable<string> permissions, int? aaudUserId = null)
    {
        if (userMothraId == null)
        {
            MockUserHelper.Setup(x => x.GetCurrentUser()).Returns((AaudUser?)null);
            return;
        }

        var testUser = CreateTestUser(userMothraId, aaudUserId);
        MockUserHelper.Setup(x => x.GetCurrentUser()).Returns(testUser);

        // Default all permissions to false
        MockUserHelper.Setup(x => x.HasPermission(It.IsAny<RAPSContext>(), It.IsAny<AaudUser>(), It.IsAny<string>()))
            .Returns(false);

        // Set up the specific permissions to return true
        foreach (var permission in permissions)
        {
            MockUserHelper.Setup(x => x.HasPermission(RapsContext, testUser, permission))
                .Returns(true);
        }
    }

    /// <summary>
    /// Adds member permissions to RAPS context
    /// </summary>
    protected void AddMemberPermissions(string mothraId, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            var permissionEntity = RapsContext.TblPermissions.FirstOrDefault(p => p.Permission == permission);
            if (permissionEntity != null)
            {
                RapsContext.TblMemberPermissions.Add(new Viper.Models.RAPS.TblMemberPermission
                {
                    MemberId = mothraId,
                    PermissionId = permissionEntity.PermissionId,
                    Access = 1,
                    StartDate = DateTime.Today.AddYears(-1),
                    EndDate = null
                });
            }
        }
        RapsContext.SaveChanges();
    }

    /// <summary>
    /// Sets up authenticated HttpContext with required services for controllers
    /// </summary>
    protected void SetupControllerContext(ControllerBase controller)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<RAPSContext>(_ => RapsContext);
        serviceCollection.AddScoped<EffortDbContext>(_ => EffortContext);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, TestUserLoginId) },
                    "test"
                )
            )
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
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
            EffortContext?.Dispose();
            RapsContext?.Dispose();
        }
    }
}
