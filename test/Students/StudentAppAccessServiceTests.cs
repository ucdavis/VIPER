using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.test.Students;

/// <summary>
/// Tests for StudentAppAccessService: opening and closing a student self-service app, which is
/// held in RAPS as the app's student permission on the DVM student role.
/// </summary>
public sealed class StudentAppAccessServiceTests : IDisposable
{
    private const string Permission = "SVMSecure.CareerSelection.Student";
    private const string OtherPermission = "SVMSecure.Students.EmergencyContactStudent";

    private readonly RAPSContext _rapsContext;
    private readonly AAUDContext _aaudContext;
    private readonly StudentAppAccessService _service;
    private readonly int _permissionId;
    private readonly int _otherPermissionId;
    private readonly int _roleId;

    public StudentAppAccessServiceTests()
    {
        _rapsContext = new RAPSContext(InMemory<RAPSContext>("RAPS"));
        _aaudContext = new AAUDContext(InMemory<AAUDContext>("AAUD"));

        var permission = new TblPermission { Permission = Permission };
        var otherPermission = new TblPermission { Permission = OtherPermission };
        var role = new TblRole { Role = StudentRoles.DvmStudents };
        _rapsContext.TblPermissions.AddRange(permission, otherPermission);
        _rapsContext.TblRoles.Add(role);
        _rapsContext.SaveChanges();
        _permissionId = permission.PermissionId;
        _otherPermissionId = otherPermission.PermissionId;
        _roleId = role.RoleId;

        var userHelper = Substitute.For<IUserHelper>();
        userHelper.GetCurrentUser().Returns(new AaudUser
        {
            AaudUserId = 1,
            ClientId = "UCD",
            MothraId = "ADMIN001",
            LoginId = "admin",
            DisplayFullName = "Test Admin",
            DisplayFirstName = "Test",
            DisplayLastName = "Admin",
            LastName = "Admin",
            FirstName = "Test"
        });
        _service = new StudentAppAccessService(_rapsContext, userHelper);
    }

    public void Dispose()
    {
        _rapsContext.Dispose();
        _aaudContext.Dispose();
    }

    [Fact]
    public async Task IsAppOpenAsync_NoGrant_ReturnsFalse()
    {
        Assert.False(await _service.IsAppOpenAsync(Permission));
    }

    [Fact]
    public async Task IsAppOpenAsync_RoleHoldsThePermission_ReturnsTrue()
    {
        await GrantToRoleAsync(_permissionId, access: 1);

        Assert.True(await _service.IsAppOpenAsync(Permission));
    }

    [Fact]
    public async Task IsAppOpenAsync_DenyRow_ReturnsFalse()
    {
        await GrantToRoleAsync(_permissionId, access: 0);

        Assert.False(await _service.IsAppOpenAsync(Permission));
    }

    [Fact]
    public async Task IsAppOpenAsync_AnotherAppsGrant_DoesNotOpenThisOne()
    {
        // The permission name is what separates the apps; they share the one DVM student role.
        await GrantToRoleAsync(_otherPermissionId, access: 1);

        Assert.False(await _service.IsAppOpenAsync(Permission));
        Assert.True(await _service.IsAppOpenAsync(OtherPermission));
    }

    [Fact]
    public async Task ToggleAppAccessAsync_WhenClosed_OpensTheApp()
    {
        var isOpen = await _service.ToggleAppAccessAsync(Permission);

        Assert.True(isOpen);
        var rolePermission = await _rapsContext.TblRolePermissions
            .SingleAsync(rp => rp.PermissionId == _permissionId, TestContext.Current.CancellationToken);
        Assert.Equal(1, rolePermission.Access);
        Assert.Equal("admin", rolePermission.ModBy);
    }

    [Fact]
    public async Task ToggleAppAccessAsync_WhenOpen_RemovesTheRowRatherThanDenying()
    {
        // A role Deny would override an individual member Allow, silently breaking individual grants.
        await GrantToRoleAsync(_permissionId, access: 1);

        var isOpen = await _service.ToggleAppAccessAsync(Permission);

        Assert.False(isOpen);
        Assert.Empty(await _rapsContext.TblRolePermissions
            .Where(rp => rp.PermissionId == _permissionId)
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ToggleAppAccessAsync_LegacyDenyRow_FlipsItToAllow()
    {
        await GrantToRoleAsync(_permissionId, access: 0);

        var isOpen = await _service.ToggleAppAccessAsync(Permission);

        Assert.True(isOpen);
        var rolePermission = await _rapsContext.TblRolePermissions
            .SingleAsync(rp => rp.PermissionId == _permissionId, TestContext.Current.CancellationToken);
        Assert.Equal(1, rolePermission.Access);
    }

    [Fact]
    public async Task ToggleAppAccessAsync_LeavesTheOtherAppAlone()
    {
        await GrantToRoleAsync(_otherPermissionId, access: 1);

        await _service.ToggleAppAccessAsync(Permission);

        Assert.True(await _service.IsAppOpenAsync(OtherPermission));
    }

    [Fact]
    public async Task GetPermissionIdAsync_ReturnsTheStoredId()
    {
        Assert.Equal(_permissionId, await _service.GetPermissionIdAsync(Permission));
    }

    [Fact]
    public async Task GetPermissionIdAsync_UnknownPermission_Throws()
    {
        // A missing RAPS row is a deployment problem, not something a caller can recover from.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetPermissionIdAsync("SVMSecure.NotReal"));

        Assert.Contains("SVMSecure.NotReal", ex.Message);
    }

    [Fact]
    public async Task IsAppOpenAsync_MissingDvmStudentRole_Throws()
    {
        _rapsContext.TblRoles.RemoveRange(_rapsContext.TblRoles);
        await _rapsContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.IsAppOpenAsync(Permission));

        Assert.Contains(StudentRoles.DvmStudents, ex.Message);
    }

    private static DbContextOptions<T> InMemory<T>(string name) where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase($"{name}_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private async Task GrantToRoleAsync(int permissionId, byte access)
    {
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _roleId,
            PermissionId = permissionId,
            Access = access,
            ModTime = DateTime.Now,
            ModBy = "seed"
        });
        await _rapsContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
