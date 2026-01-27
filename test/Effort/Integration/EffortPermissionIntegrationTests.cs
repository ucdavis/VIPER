using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort.Integration;

/// <summary>
/// Integration tests for the Effort permission service architecture.
/// Tests the complete flow of permission checks for department-level,
/// full access, and self-service permission models.
/// </summary>
public class EffortPermissionIntegrationTests : EffortIntegrationTestBase
{
    private readonly EffortPermissionService _permissionService;

    public EffortPermissionIntegrationTests()
    {
        _permissionService = new EffortPermissionService(
            EffortContext,
            RapsContext,
            MockUserHelper.Object
        );
    }

    #region Full Access (ViewAllDepartments) Tests

    [Fact]
    public async Task FullAccess_HasFullAccessAsync_ReturnsTrue()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var hasFullAccess = await _permissionService.HasFullAccessAsync();

        // Assert
        Assert.True(hasFullAccess);
    }

    [Fact]
    public async Task FullAccess_CanViewAnyDepartment()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var canViewDvm = await _permissionService.CanViewDepartmentAsync(DvmDepartment);
        var canViewVme = await _permissionService.CanViewDepartmentAsync(VmeDepartment);
        var canViewApc = await _permissionService.CanViewDepartmentAsync(ApcDepartment);

        // Assert
        Assert.True(canViewDvm);
        Assert.True(canViewVme);
        Assert.True(canViewApc);
    }

    [Fact]
    public async Task FullAccess_CanEditAnyDepartment()
    {
        // Arrange - Full access user also needs EditEffort permission
        SetupUserWithFullAccess();

        // Act
        var canEditDvm = await _permissionService.CanEditDepartmentAsync(DvmDepartment);
        var canEditVme = await _permissionService.CanEditDepartmentAsync(VmeDepartment);

        // Assert - Full access bypasses department filtering
        Assert.True(canEditDvm);
        Assert.True(canEditVme);
    }

    [Fact]
    public async Task FullAccess_GetAuthorizedDepartmentsAsync_ReturnsEmptyList()
    {
        // Arrange - Full access means ALL departments
        SetupUserWithFullAccess();

        // Act
        var departments = await _permissionService.GetAuthorizedDepartmentsAsync();

        // Assert - Empty list indicates full access (all departments)
        Assert.Empty(departments);
    }

    [Fact]
    public async Task FullAccess_CanViewAnyPersonEffort()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act - Can view any person regardless of department
        var canViewDvmPerson = await _permissionService.CanViewPersonEffortAsync(TestUserAaudId, TestTermCode);
        var canViewVmePerson = await _permissionService.CanViewPersonEffortAsync(1001, TestTermCode);
        var canViewApcPerson = await _permissionService.CanViewPersonEffortAsync(1002, TestTermCode);

        // Assert
        Assert.True(canViewDvmPerson);
        Assert.True(canViewVmePerson);
        Assert.True(canViewApcPerson);
    }

    #endregion

    #region Department-Level Access (ViewDept) Tests

    [Fact]
    public async Task DepartmentAccess_HasDepartmentLevelAccessAsync_ReturnsTrue()
    {
        // Arrange
        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Act
        var hasDeptAccess = await _permissionService.HasDepartmentLevelAccessAsync();

        // Assert
        Assert.True(hasDeptAccess);
    }

    [Fact]
    public async Task DepartmentAccess_CanViewOnlyAuthorizedDepartments()
    {
        // Arrange - User only has access to DVM
        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Act
        var canViewDvm = await _permissionService.CanViewDepartmentAsync(DvmDepartment);
        var canViewVme = await _permissionService.CanViewDepartmentAsync(VmeDepartment);
        var canViewApc = await _permissionService.CanViewDepartmentAsync(ApcDepartment);

        // Assert
        Assert.True(canViewDvm);
        Assert.False(canViewVme);
        Assert.False(canViewApc);
    }

    [Fact]
    public async Task DepartmentAccess_CanViewMultipleAuthorizedDepartments()
    {
        // Arrange - User has access to DVM and VME
        SetupUserWithDepartmentViewAccess(DvmDepartment, VmeDepartment);

        // Act
        var canViewDvm = await _permissionService.CanViewDepartmentAsync(DvmDepartment);
        var canViewVme = await _permissionService.CanViewDepartmentAsync(VmeDepartment);
        var canViewApc = await _permissionService.CanViewDepartmentAsync(ApcDepartment);

        // Assert
        Assert.True(canViewDvm);
        Assert.True(canViewVme);
        Assert.False(canViewApc);
    }

    [Fact]
    public async Task DepartmentAccess_GetAuthorizedDepartmentsAsync_ReturnsUserDepartments()
    {
        // Arrange
        SetupUserWithDepartmentViewAccess(DvmDepartment, VmeDepartment);

        // Act
        var departments = await _permissionService.GetAuthorizedDepartmentsAsync();

        // Assert
        Assert.Equal(2, departments.Count);
        Assert.Contains(DvmDepartment, departments);
        Assert.Contains(VmeDepartment, departments);
    }

    [Fact]
    public async Task DepartmentAccess_CanOnlyViewPersonsInAuthorizedDepartment()
    {
        // Arrange - User only has access to DVM
        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Act
        var canViewDvmPerson = await _permissionService.CanViewPersonEffortAsync(TestUserAaudId, TestTermCode);
        var canViewVmePerson = await _permissionService.CanViewPersonEffortAsync(1001, TestTermCode);
        var canViewApcPerson = await _permissionService.CanViewPersonEffortAsync(1002, TestTermCode);

        // Assert
        Assert.True(canViewDvmPerson);
        Assert.False(canViewVmePerson);
        Assert.False(canViewApcPerson);
    }

    #endregion

    #region Edit Permission Tests

    [Fact]
    public async Task EditEffort_CanOnlyEditAuthorizedDepartments()
    {
        // Arrange - User has edit permission for DVM only
        SetupUserWithEditEffortPermission(DvmDepartment);

        // Act
        var canEditDvm = await _permissionService.CanEditDepartmentAsync(DvmDepartment);
        var canEditVme = await _permissionService.CanEditDepartmentAsync(VmeDepartment);

        // Assert
        Assert.True(canEditDvm);
        Assert.False(canEditVme);
    }

    [Fact]
    public async Task EditEffort_CanOnlyEditPersonsInAuthorizedDepartment()
    {
        // Arrange - User has edit permission for DVM only
        SetupUserWithEditEffortPermission(DvmDepartment);

        // Act
        var canEditDvmPerson = await _permissionService.CanEditPersonEffortAsync(TestUserAaudId, TestTermCode);
        var canEditVmePerson = await _permissionService.CanEditPersonEffortAsync(1001, TestTermCode);

        // Assert
        Assert.True(canEditDvmPerson);
        Assert.False(canEditVmePerson);
    }

    [Fact]
    public async Task EditEffort_WithoutPermission_CannotEditAnyDepartment()
    {
        // Arrange - User has only ViewDept, not EditEffort
        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Act
        var canEditDvm = await _permissionService.CanEditDepartmentAsync(DvmDepartment);

        // Assert - Cannot edit even their own department without EditEffort permission
        Assert.False(canEditDvm);
    }

    #endregion

    #region Self-Service Access (VerifyEffort) Tests

    [Fact]
    public async Task SelfService_HasSelfServiceAccessAsync_ReturnsTrue()
    {
        // Arrange
        SetupUserWithSelfServiceAccess();

        // Act
        var hasSelfService = await _permissionService.HasSelfServiceAccessAsync();

        // Assert
        Assert.True(hasSelfService);
    }

    [Fact]
    public async Task SelfService_CanViewOwnEffort()
    {
        // Arrange - Self-service user can only view their own effort
        SetupUserWithSelfServiceAccess();

        // Act
        var canViewOwn = await _permissionService.CanViewPersonEffortAsync(TestUserAaudId, TestTermCode);

        // Assert
        Assert.True(canViewOwn);
    }

    [Fact]
    public async Task SelfService_CannotViewOtherPersonEffort()
    {
        // Arrange - Self-service only
        SetupUserWithSelfServiceAccess();

        // Act
        var canViewOther = await _permissionService.CanViewPersonEffortAsync(1001, TestTermCode);

        // Assert
        Assert.False(canViewOther);
    }

    [Fact]
    public void SelfService_IsCurrentUser_ReturnsTrue()
    {
        // Arrange
        SetupUserWithSelfServiceAccess();

        // Act
        var isCurrentUser = _permissionService.IsCurrentUser(TestUserAaudId);

        // Assert
        Assert.True(isCurrentUser);
    }

    [Fact]
    public void SelfService_IsCurrentUser_ReturnsFalseForOtherPerson()
    {
        // Arrange
        SetupUserWithSelfServiceAccess();

        // Act
        var isCurrentUser = _permissionService.IsCurrentUser(1001);

        // Assert
        Assert.False(isCurrentUser);
    }

    #endregion

    #region No Permission / Null User Tests

    [Fact]
    public async Task NoPermissions_HasFullAccessAsync_ReturnsFalse()
    {
        // Arrange
        SetupUserWithNoPermissions();

        // Act
        var hasFullAccess = await _permissionService.HasFullAccessAsync();

        // Assert
        Assert.False(hasFullAccess);
    }

    [Fact]
    public async Task NoPermissions_HasDepartmentLevelAccessAsync_ReturnsFalse()
    {
        // Arrange
        SetupUserWithNoPermissions();

        // Act
        var hasDeptAccess = await _permissionService.HasDepartmentLevelAccessAsync();

        // Assert
        Assert.False(hasDeptAccess);
    }

    [Fact]
    public async Task NoPermissions_CannotViewAnyDepartment()
    {
        // Arrange
        SetupUserWithNoPermissions();

        // Act
        var canViewDvm = await _permissionService.CanViewDepartmentAsync(DvmDepartment);
        var canViewVme = await _permissionService.CanViewDepartmentAsync(VmeDepartment);

        // Assert
        Assert.False(canViewDvm);
        Assert.False(canViewVme);
    }

    [Fact]
    public async Task NoPermissions_CannotViewAnyPersonEffort()
    {
        // Arrange
        SetupUserWithNoPermissions();

        // Act
        var canViewPerson = await _permissionService.CanViewPersonEffortAsync(TestUserAaudId, TestTermCode);

        // Assert
        Assert.False(canViewPerson);
    }

    [Fact]
    public async Task NullUser_HasFullAccessAsync_ReturnsFalse()
    {
        // Arrange
        SetupNullUser();

        // Act
        var hasFullAccess = await _permissionService.HasFullAccessAsync();

        // Assert
        Assert.False(hasFullAccess);
    }

    [Fact]
    public async Task NullUser_CannotViewAnyDepartment()
    {
        // Arrange
        SetupNullUser();

        // Act
        var canViewDvm = await _permissionService.CanViewDepartmentAsync(DvmDepartment);

        // Assert
        Assert.False(canViewDvm);
    }

    [Fact]
    public void NullUser_GetCurrentPersonId_ReturnsZero()
    {
        // Arrange
        SetupNullUser();

        // Act
        var personId = _permissionService.GetCurrentPersonId();

        // Assert
        Assert.Equal(0, personId);
    }

    #endregion

    #region Base Permission Only Tests

    [Fact]
    public async Task BasePermissionOnly_HasFullAccessAsync_ReturnsFalse()
    {
        // Arrange
        SetupUserWithBasePermissionOnly();

        // Act
        var hasFullAccess = await _permissionService.HasFullAccessAsync();

        // Assert
        Assert.False(hasFullAccess);
    }

    [Fact]
    public async Task BasePermissionOnly_HasDepartmentLevelAccessAsync_ReturnsFalse()
    {
        // Arrange - Base permission does not include ViewDept
        SetupUserWithBasePermissionOnly();

        // Act
        var hasDeptAccess = await _permissionService.HasDepartmentLevelAccessAsync();

        // Assert
        Assert.False(hasDeptAccess);
    }

    [Fact]
    public async Task BasePermissionOnly_CannotViewAnyDepartment()
    {
        // Arrange
        SetupUserWithBasePermissionOnly();

        // Act
        var canViewDvm = await _permissionService.CanViewDepartmentAsync(DvmDepartment);

        // Assert
        Assert.False(canViewDvm);
    }

    #endregion

    #region Permission Hierarchy Tests

    [Fact]
    public async Task FullAccess_OverridesDepartmentLevelAccess()
    {
        // Arrange - User has ViewAllDepartments (full access)
        SetupUserWithFullAccess();

        // Act - Should have access to all departments regardless of UserAccess entries
        var hasDeptAccess = await _permissionService.HasDepartmentLevelAccessAsync();
        var canViewAllDepts = await _permissionService.CanViewDepartmentAsync(ApcDepartment);

        // Assert
        Assert.False(hasDeptAccess); // ViewDept is not set
        Assert.True(canViewAllDepts); // But full access grants view to all
    }

    [Fact]
    public async Task DepartmentAccess_CaseInsensitive()
    {
        // Arrange
        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Act - Test case insensitivity
        var canViewUppercase = await _permissionService.CanViewDepartmentAsync("DVM");
        var canViewLowercase = await _permissionService.CanViewDepartmentAsync("dvm");
        var canViewMixed = await _permissionService.CanViewDepartmentAsync("Dvm");

        // Assert
        Assert.True(canViewUppercase);
        Assert.True(canViewLowercase);
        Assert.True(canViewMixed);
    }

    [Fact]
    public async Task InactiveUserAccess_IsIgnored()
    {
        // Arrange - Add inactive UserAccess entry
        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Add an inactive entry for VME
        EffortContext.UserAccess.Add(new UserAccess
        {
            PersonId = TestUserAaudId,
            DepartmentCode = VmeDepartment,
            IsActive = false // Inactive!
        });
        await EffortContext.SaveChangesAsync();

        // Act
        var canViewVme = await _permissionService.CanViewDepartmentAsync(VmeDepartment);

        // Assert - Inactive access should be ignored
        Assert.False(canViewVme);
    }

    #endregion

    #region Person Effort with ReportUnit Tests

    [Fact]
    public async Task DepartmentAccess_CanViewPersonByReportUnit()
    {
        // Arrange - Add a person with different EffortDept but matching ReportUnit
        var personWithReportUnit = new EffortPerson
        {
            PersonId = 2000,
            TermCode = TestTermCode,
            FirstName = "Charlie",
            LastName = "Brown",
            EffortDept = ApcDepartment, // Different from user's department
            ReportUnit = DvmDepartment  // But ReportUnit matches
        };
        EffortContext.Persons.Add(personWithReportUnit);
        await EffortContext.SaveChangesAsync();

        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Act
        var canViewPerson = await _permissionService.CanViewPersonEffortAsync(2000, TestTermCode);

        // Assert - Should be able to view via ReportUnit match
        Assert.True(canViewPerson);
    }

    [Fact]
    public async Task DepartmentAccess_CannotViewPersonWithNoMatchingDeptOrReportUnit()
    {
        // Arrange - Person in completely different department with no matching ReportUnit
        SetupUserWithDepartmentViewAccess(DvmDepartment);

        // Act
        var canViewApcPerson = await _permissionService.CanViewPersonEffortAsync(1002, TestTermCode);

        // Assert
        Assert.False(canViewApcPerson);
    }

    #endregion
}
