using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.test.Students;

/// <summary>
/// Tests for EmergencyContactService: completeness calculation, phone utilities,
/// and core service operations using InMemory databases.
/// </summary>
public sealed class EmergencyContactServiceTests : IDisposable
{
    private readonly SISContext _sisContext;
    private readonly RAPSContext _rapsContext;
    private readonly AAUDContext _aaudContext;

    // IDs assigned by InMemory DB — seeded in constructor
    private readonly int _seededPermissionId;
    private readonly int _seededRoleId;

    public EmergencyContactServiceTests()
    {
        var sisOptions = new DbContextOptionsBuilder<SISContext>()
            .UseInMemoryDatabase(databaseName: "SIS_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _sisContext = new SISContext(sisOptions);

        var rapsOptions = new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase(databaseName: "RAPS_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _rapsContext = new RAPSContext(rapsOptions);

        var aaudOptions = new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase(databaseName: "AAUD_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _aaudContext = new AAUDContext(aaudOptions);

        // Seed the RAPS permission and role so string-based lookups work
        var permission = new TblPermission { Permission = EmergencyContactPermissions.Student };
        _rapsContext.TblPermissions.Add(permission);
        var role = new TblRole { Role = EmergencyContactPermissions.StudentRoleName };
        _rapsContext.TblRoles.Add(role);
        _rapsContext.SaveChanges();
        _seededPermissionId = permission.PermissionId;
        _seededRoleId = role.RoleId;

    }

    public void Dispose()
    {
        _sisContext.Dispose();
        _rapsContext.Dispose();
        _aaudContext.Dispose();
    }

    #region Phone Normalization Tests

    [Fact]
    public void NormalizePhone_NullInput_ReturnsNull()
    {
        Assert.Null(PhoneHelper.NormalizePhone(null));
    }

    [Fact]
    public void NormalizePhone_EmptyInput_ReturnsNull()
    {
        Assert.Null(PhoneHelper.NormalizePhone(""));
    }

    [Fact]
    public void NormalizePhone_WhitespaceInput_ReturnsNull()
    {
        Assert.Null(PhoneHelper.NormalizePhone("   "));
    }

    [Fact]
    public void NormalizePhone_FormattedUSPhone_ReturnsDigits()
    {
        Assert.Equal("5305551234", PhoneHelper.NormalizePhone("(530) 555-1234"));
    }

    [Fact]
    public void NormalizePhone_TenDigits_ReturnsSame()
    {
        Assert.Equal("5305551234", PhoneHelper.NormalizePhone("5305551234"));
    }

    [Fact]
    public void NormalizePhone_SevenDigits_ReturnsSame()
    {
        Assert.Equal("5551234", PhoneHelper.NormalizePhone("5551234"));
    }

    [Fact]
    public void NormalizePhone_LettersOnly_ReturnsNull()
    {
        Assert.Null(PhoneHelper.NormalizePhone("no phone"));
    }

    [Fact]
    public void NormalizePhone_PartialDigits_ReturnsNull()
    {
        Assert.Null(PhoneHelper.NormalizePhone("12345"));
    }

    [Fact]
    public void NormalizePhone_SixDigits_ReturnsNull()
    {
        Assert.Null(PhoneHelper.NormalizePhone("530555"));
    }

    [Theory]
    [InlineData("+1 (530) 555-1234")]
    [InlineData("+1 530 555 1234")]
    [InlineData("1-530-555-1234")]
    [InlineData("15305551234")]
    public void NormalizePhone_ElevenDigitsWithLeadingOne_ReturnsNull(string input)
    {
        // Documents current behavior: inputs with the US country code prefix strip
        // to 11 digits, which is neither 7 nor 10, so normalization rejects them.
        // If this changes to accept +1-prefixed numbers, update this test.
        Assert.Null(PhoneHelper.NormalizePhone(input));
    }

    [Theory]
    [InlineData("+1 (530) 555-1234")]
    [InlineData("15305551234")]
    public void IsValidPhone_ElevenDigits_ReturnsFalse(string input)
    {
        Assert.False(PhoneHelper.IsValidPhone(input));
    }

    #endregion

    #region Phone Formatting Tests

    [Fact]
    public void FormatPhone_NullInput_ReturnsNull()
    {
        Assert.Null(PhoneHelper.FormatPhone(null));
    }

    [Fact]
    public void FormatPhone_EmptyInput_ReturnsNull()
    {
        Assert.Null(PhoneHelper.FormatPhone(""));
    }

    [Fact]
    public void FormatPhone_TenDigits_ReturnsNationalFormat()
    {
        Assert.Equal("(530) 555-1234", PhoneHelper.FormatPhone("5305551234"));
    }

    [Fact]
    public void FormatPhone_SevenDigits_ReturnsFormatted()
    {
        Assert.Equal("555-1234", PhoneHelper.FormatPhone("5551234"));
    }

    [Fact]
    public void FormatPhone_FormattedInput_ReturnsFormatted()
    {
        Assert.Equal("(530) 555-1234", PhoneHelper.FormatPhone("(530) 555-1234"));
    }

    [Fact]
    public void FormatPhone_InvalidLength_ReturnsAsIs()
    {
        Assert.Equal("12345", PhoneHelper.FormatPhone("12345"));
    }

    #endregion

    #region Phone Validation Tests

    [Fact]
    public void IsValidPhone_ValidUSNumber_ReturnsTrue()
    {
        Assert.True(PhoneHelper.IsValidPhone("(530) 555-1234"));
    }

    [Fact]
    public void IsValidPhone_SevenDigits_ReturnsTrue()
    {
        Assert.True(PhoneHelper.IsValidPhone("5551234"));
    }

    [Fact]
    public void IsValidPhone_Empty_ReturnsTrue()
    {
        Assert.True(PhoneHelper.IsValidPhone(""));
    }

    [Fact]
    public void IsValidPhone_Null_ReturnsTrue()
    {
        Assert.True(PhoneHelper.IsValidPhone(null));
    }

    [Fact]
    public void IsValidPhone_Invalid_ReturnsFalse()
    {
        Assert.False(PhoneHelper.IsValidPhone("12345"));
    }

    #endregion

    #region Student Info Completeness Tests

    [Fact]
    public void GetStudentInfoMissingFields_AllEmpty_ReturnsAllMissing()
    {
        var contact = new StudentContact();
        var missing = EmergencyContactService.GetStudentInfoMissingFields(contact);
        Assert.Equal(2, missing.Count);
        Assert.Contains("Address", missing);
        Assert.Contains("Phone", missing);
    }

    [Fact]
    public void GetStudentInfoMissingFields_AddressOnly_MissingPhoneAndFullAddress()
    {
        // Address alone is not enough; need address+city+zip
        var contact = new StudentContact { Address = "One Shields Avenue" };
        var missing = EmergencyContactService.GetStudentInfoMissingFields(contact);
        Assert.Contains("Address", missing);
        Assert.Contains("Phone", missing);
    }

    [Fact]
    public void GetStudentInfoMissingFields_AddressCityZip_MissingPhone()
    {
        var contact = new StudentContact
        {
            Address = "One Shields Avenue",
            City = "Davis",
            Zip = "95616"
        };
        var missing = EmergencyContactService.GetStudentInfoMissingFields(contact);
        Assert.DoesNotContain("Address", missing);
        Assert.Contains("Phone", missing);
    }

    [Fact]
    public void GetStudentInfoMissingFields_AllFields_ReturnsEmpty()
    {
        var contact = new StudentContact
        {
            Address = "One Shields Avenue",
            City = "Davis",
            Zip = "95616",
            HomePhone = "5305551234",
            CellPhone = "5305555678"
        };
        Assert.Empty(EmergencyContactService.GetStudentInfoMissingFields(contact));
    }

    [Fact]
    public void GetStudentInfoMissingFields_CellPhoneOnly_PhoneComplete()
    {
        var contact = new StudentContact { CellPhone = "5305555678" };
        var missing = EmergencyContactService.GetStudentInfoMissingFields(contact);
        Assert.DoesNotContain("Phone", missing);
        Assert.Contains("Address", missing);
    }

    [Fact]
    public void GetStudentInfoMissingFields_HomePhoneOnly_PhoneComplete()
    {
        var contact = new StudentContact { HomePhone = "5305551234" };
        var missing = EmergencyContactService.GetStudentInfoMissingFields(contact);
        Assert.DoesNotContain("Phone", missing);
    }

    #endregion

    #region Contact Completeness Tests

    [Fact]
    public void GetContactMissingFields_NullContact_ReturnsAllMissing()
    {
        var missing = EmergencyContactService.GetContactMissingFields(null);
        Assert.Equal(4, missing.Count);
    }

    [Fact]
    public void GetContactMissingFields_EmptyContact_ReturnsAllMissing()
    {
        var contact = new StudentEmergencyContact { Type = "local" };
        var missing = EmergencyContactService.GetContactMissingFields(contact);
        Assert.Equal(4, missing.Count);
    }

    [Fact]
    public void GetContactMissingFields_NameAndCellPhone_MissingRelationshipAndEmail()
    {
        var contact = new StudentEmergencyContact
        {
            Type = "emergency",
            Name = "Jane Doe",
            CellPhone = "5305551234"
        };
        var missing = EmergencyContactService.GetContactMissingFields(contact);
        Assert.Equal(2, missing.Count);
        Assert.Contains("Relationship", missing);
        Assert.Contains("Email", missing);
    }

    [Fact]
    public void GetContactMissingFields_AllFields_ReturnsEmpty()
    {
        var contact = new StudentEmergencyContact
        {
            Type = "local",
            Name = "John Smith",
            Relationship = "Father",
            WorkPhone = "5305551111",
            Email = "john@example.com"
        };
        Assert.Empty(EmergencyContactService.GetContactMissingFields(contact));
    }

    [Fact]
    public void GetContactMissingFields_OnlyWorkPhone_PhoneComplete()
    {
        var contact = new StudentEmergencyContact
        {
            Type = "emergency",
            Name = "Jane",
            Relationship = "Mother",
            WorkPhone = "5305551111",
            Email = "jane@example.com"
        };
        Assert.Empty(EmergencyContactService.GetContactMissingFields(contact));
    }

    [Fact]
    public void GetContactMissingFields_WhitespaceFields_ReturnsAllMissing()
    {
        var contact = new StudentEmergencyContact
        {
            Type = "emergency",
            Name = "  ",
            Relationship = "",
            Email = null
        };
        Assert.Equal(4, EmergencyContactService.GetContactMissingFields(contact).Count);
    }

    #endregion

    #region IsAppOpen Tests

    [Fact]
    public async Task IsAppOpenAsync_NoRolePermission_ReturnsFalse()
    {
        var service = CreateService();
        var result = await service.IsAppOpenAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task IsAppOpenAsync_RolePermissionExists_ReturnsTrue()
    {
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.IsAppOpenAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task IsAppOpenAsync_AccessZero_ReturnsFalse()
    {
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 0
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.IsAppOpenAsync();
        Assert.False(result);
    }

    #endregion

    #region ToggleAppAccess Tests

    [Fact]
    public async Task ToggleAppAccessAsync_WhenNoRowExists_CreatesWithAccessOne()
    {
        var service = CreateService();

        var result = await service.ToggleAppAccessAsync();

        Assert.True(result);
        var rp = await _rapsContext.TblRolePermissions.SingleAsync(rp =>
            rp.RoleId == _seededRoleId
            && rp.PermissionId == _seededPermissionId);
        Assert.Equal(1, rp.Access);
    }

    [Fact]
    public async Task ToggleAppAccessAsync_WhenOpen_RemovesRolePermissionRow()
    {
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.ToggleAppAccessAsync();

        Assert.False(result);
        // Row is deleted on close so a role-level Deny cannot shadow individual grants.
        var rp = await _rapsContext.TblRolePermissions.FirstOrDefaultAsync(rp =>
            rp.RoleId == _seededRoleId
            && rp.PermissionId == _seededPermissionId);
        Assert.Null(rp);
    }

    [Fact]
    public async Task ToggleAppAccessAsync_Close_DoesNotLeaveDenyRowThatShadowsIndividualGrants()
    {
        // Seed: app open AND a student with an individual grant
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = "granted-student",
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        await service.ToggleAppAccessAsync(); // close

        // No role-permission row should remain — a row with Access=0 would Deny
        // all members, overriding the individual grant's Access=1 (RAPS semantics).
        var rolePerm = await _rapsContext.TblRolePermissions.FirstOrDefaultAsync(rp =>
            rp.RoleId == _seededRoleId && rp.PermissionId == _seededPermissionId);
        Assert.Null(rolePerm);

        // Individual grant is preserved
        var grant = await _rapsContext.TblMemberPermissions.SingleAsync(mp =>
            mp.MemberId == "granted-student" && mp.PermissionId == _seededPermissionId);
        Assert.Equal(1, grant.Access);
    }

    [Fact]
    public async Task ToggleAppAccessAsync_WhenClosed_SetsAccessOne()
    {
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 0
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.ToggleAppAccessAsync();

        Assert.True(result);
        var rp = await _rapsContext.TblRolePermissions.SingleAsync(rp =>
            rp.RoleId == _seededRoleId
            && rp.PermissionId == _seededPermissionId);
        Assert.Equal(1, rp.Access);
    }

    #endregion

    #region ToggleIndividualAccess Tests

    [Fact]
    public async Task ToggleIndividualAccessAsync_NoUser_ThrowsInvalidOperation()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ToggleIndividualAccessAsync(99999));
    }

    [Fact]
    public async Task ToggleIndividualAccessAsync_NewGrant_ReturnsTrue()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M100", 100, "Student", "Test", "V1", "12100", "student1@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var result = await service.ToggleIndividualAccessAsync(100);

            Assert.True(result);
            Assert.True(await _rapsContext.TblMemberPermissions.AnyAsync(mp =>
                mp.MemberId == "M100"
                && mp.PermissionId == _seededPermissionId));
        }
    }

    [Fact]
    public async Task ToggleIndividualAccessAsync_ExistingGrant_RemovesAndReturnsFalse()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M101", 101, "Student2", "Test", "V1", "12101", "student2@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
            {
                MemberId = "M101",
                PermissionId = _seededPermissionId,
                Access = 1
            });
            await _rapsContext.SaveChangesAsync();

            var result = await service.ToggleIndividualAccessAsync(101);

            Assert.False(result);
            Assert.False(await _rapsContext.TblMemberPermissions.AnyAsync(mp =>
                mp.MemberId == "M101"
                && mp.PermissionId == _seededPermissionId));
        }
    }

    #endregion

    #region CanEdit Tests

    [Fact]
    public async Task CanEditAsync_NullLoginId_ReturnsFalse()
    {
        var service = CreateService();
        var result = await service.CanEditAsync(1, null);
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_EmptyLoginId_ReturnsFalse()
    {
        var service = CreateService();
        var result = await service.CanEditAsync(1, "");
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_UnknownUser_ReturnsFalse()
    {
        var service = CreateService();
        var result = await service.CanEditAsync(1, "nonexistent");
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_Admin_ReturnsTrue()
    {
        var service = CreateServiceWithPermissions(EmergencyContactPermissions.Admin);
        _aaudContext.AaudUsers.Add(CreateTestUser(200, "admin200"));
        await _aaudContext.SaveChangesAsync();

        var result = await service.CanEditAsync(999, "admin200");
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditAsync_SisUser_ReturnsFalse()
    {
        var service = CreateServiceWithPermissions(EmergencyContactPermissions.SISAllStudents);
        _aaudContext.AaudUsers.Add(CreateTestUser(201, "sis201"));
        await _aaudContext.SaveChangesAsync();

        var result = await service.CanEditAsync(201, "sis201");
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_StudentOwnRecord_AppOpen_ReturnsTrue()
    {
        var service = CreateServiceWithPermissions();
        _aaudContext.AaudUsers.Add(CreateTestUser(202, "stu202"));
        await _aaudContext.SaveChangesAsync();

        // Open the app
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var result = await service.CanEditAsync(202, "stu202");
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditAsync_StudentOwnRecord_IndividualGrant_ReturnsTrue()
    {
        var user = CreateTestUser(203, "stu203");
        var service = CreateServiceWithPermissions();
        _aaudContext.AaudUsers.Add(user);
        await _aaudContext.SaveChangesAsync();

        // App closed, but individual grant exists
        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = user.MothraId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var result = await service.CanEditAsync(203, "stu203");
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditAsync_StudentOwnRecord_AppClosed_NoGrant_ReturnsFalse()
    {
        var service = CreateServiceWithPermissions();
        _aaudContext.AaudUsers.Add(CreateTestUser(204, "stu204"));
        await _aaudContext.SaveChangesAsync();

        var result = await service.CanEditAsync(204, "stu204");
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_StudentOtherRecord_ReturnsFalse()
    {
        var service = CreateServiceWithPermissions();
        _aaudContext.AaudUsers.Add(CreateTestUser(205, "stu205"));
        await _aaudContext.SaveChangesAsync();

        // Open the app — but student tries to edit someone else's record
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var result = await service.CanEditAsync(999, "stu205");
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_StudentOwnRecord_ExpiredIndividualGrant_ReturnsFalse()
    {
        var user = CreateTestUser(206, "stu206");
        var service = CreateServiceWithPermissions();
        _aaudContext.AaudUsers.Add(user);
        await _aaudContext.SaveChangesAsync();

        // App closed, grant exists but ended yesterday
        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = user.MothraId,
            PermissionId = _seededPermissionId,
            Access = 1,
            EndDate = DateTime.Now.AddDays(-1)
        });
        await _rapsContext.SaveChangesAsync();

        var result = await service.CanEditAsync(206, "stu206");
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_StudentOwnRecord_FutureIndividualGrant_ReturnsFalse()
    {
        var user = CreateTestUser(207, "stu207");
        var service = CreateServiceWithPermissions();
        _aaudContext.AaudUsers.Add(user);
        await _aaudContext.SaveChangesAsync();

        // Grant doesn't start until tomorrow
        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = user.MothraId,
            PermissionId = _seededPermissionId,
            Access = 1,
            StartDate = DateTime.Now.AddDays(1)
        });
        await _rapsContext.SaveChangesAsync();

        var result = await service.CanEditAsync(207, "stu207");
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditAsync_StudentOwnRecord_MemberAccessZero_ReturnsFalse()
    {
        var user = CreateTestUser(208, "stu208");
        var service = CreateServiceWithPermissions();
        _aaudContext.AaudUsers.Add(user);
        await _aaudContext.SaveChangesAsync();

        // Member row with Access=0 should not count as a grant
        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = user.MothraId,
            PermissionId = _seededPermissionId,
            Access = 0
        });
        await _rapsContext.SaveChangesAsync();

        var result = await service.CanEditAsync(208, "stu208");
        Assert.False(result);
    }

    #endregion

    #region GetAccessStatus Tests

    [Fact]
    public async Task GetAccessStatusAsync_AppOpen_ReturnsTrue()
    {
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var status = await service.GetAccessStatusAsync();

        Assert.True(status.AppOpen);
    }

    [Fact]
    public async Task GetAccessStatusAsync_AppClosed_ReturnsFalse()
    {
        var service = CreateService();
        var status = await service.GetAccessStatusAsync();

        Assert.False(status.AppOpen);
    }

    [Fact]
    public async Task GetAccessStatusAsync_WithIndividualGrants_ReturnsGrantedStudents()
    {
        _aaudContext.AaudUsers.Add(CreateTestUser(300, "stu300"));
        await _aaudContext.SaveChangesAsync();

        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = "M300",
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var status = await service.GetAccessStatusAsync();

        Assert.Single(status.IndividualGrants);
        Assert.Equal(300, status.IndividualGrants[0].PersonId);
    }

    [Fact]
    public async Task GetAccessStatusAsync_ExpiredGrant_Excluded()
    {
        _aaudContext.AaudUsers.Add(CreateTestUser(301, "stu301"));
        await _aaudContext.SaveChangesAsync();

        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = "M301",
            PermissionId = _seededPermissionId,
            Access = 1,
            EndDate = DateTime.Now.AddDays(-1)
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var status = await service.GetAccessStatusAsync();

        Assert.Empty(status.IndividualGrants);
    }

    [Fact]
    public async Task GetAccessStatusAsync_NoGrants_ReturnsEmptyList()
    {
        var service = CreateService();
        var status = await service.GetAccessStatusAsync();

        Assert.Empty(status.IndividualGrants);
    }

    [Fact]
    public async Task GetAccessStatusAsync_IndependentOfAppState()
    {
        // Grant survives both app open and app closed
        _aaudContext.AaudUsers.Add(CreateTestUser(302, "stu302"));
        await _aaudContext.SaveChangesAsync();
        _rapsContext.TblMemberPermissions.Add(new TblMemberPermission
        {
            MemberId = "M302",
            PermissionId = _seededPermissionId,
            Access = 1
        });
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var statusOpen = await service.GetAccessStatusAsync();
        Assert.True(statusOpen.AppOpen);
        Assert.Single(statusOpen.IndividualGrants);

        // Close app (removes role-permission row); grant unchanged
        _rapsContext.TblRolePermissions.RemoveRange(_rapsContext.TblRolePermissions);
        await _rapsContext.SaveChangesAsync();

        var statusClosed = await service.GetAccessStatusAsync();
        Assert.False(statusClosed.AppOpen);
        Assert.Single(statusClosed.IndividualGrants);
        Assert.Equal(302, statusClosed.IndividualGrants[0].PersonId);
    }

    #endregion

    #region Access Workflow Integration

    [Fact]
    public async Task Workflow_GrantThenCloseApp_StudentCanStillEdit()
    {
        // Simulates: admin opens app, grants individual access, then closes app.
        // Individual grant must survive the close (no role-level Deny row left behind).
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M400", 400, "First", "Student", "V1", "12400", "s400@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            // 1. Open app
            Assert.True(await service.ToggleAppAccessAsync());
            Assert.True(await service.CanEditAsync(400, "user400"));

            // 2. Grant individual while app is open (redundant but legal)
            Assert.True(await service.ToggleIndividualAccessAsync(400));

            // 3. Close app — role-permission row should be removed, not flipped to 0
            Assert.False(await service.ToggleAppAccessAsync());
            Assert.False(await service.IsAppOpenAsync());

            // 4. Student can still edit because of the individual grant
            Assert.True(await service.CanEditAsync(400, "user400"));

            // 5. Revoke grant → student loses edit
            Assert.False(await service.ToggleIndividualAccessAsync(400));
            Assert.False(await service.CanEditAsync(400, "user400"));
        }
    }

    [Fact]
    public async Task Workflow_ReopenApp_AfterLegacyAccessZeroRow_FlipsToOpen()
    {
        // Legacy data case: a row with Access=0 exists from the previous toggle
        // behavior. Re-opening should set it to 1 (not insert a duplicate row).
        _rapsContext.TblRolePermissions.Add(new TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 0
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        Assert.True(await service.ToggleAppAccessAsync());
        Assert.True(await service.IsAppOpenAsync());

        var rows = await _rapsContext.TblRolePermissions
            .Where(rp => rp.RoleId == _seededRoleId && rp.PermissionId == _seededPermissionId)
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(1, rows[0].Access);
    }

    [Fact]
    public async Task Workflow_ToggleIndividualAccess_Idempotent()
    {
        // Repeat grant/revoke cycles should land back in the expected state each time
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M401", 401, "Second", "Student", "V1", "12401", "s401@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            Assert.True(await service.ToggleIndividualAccessAsync(401));   // grant
            Assert.False(await service.ToggleIndividualAccessAsync(401));  // revoke
            Assert.True(await service.ToggleIndividualAccessAsync(401));   // grant again
            Assert.False(await service.ToggleIndividualAccessAsync(401));  // revoke again

            // Final state: no grant for this student
            var grants = await _rapsContext.TblMemberPermissions
                .Where(mp => mp.MemberId == "M401" && mp.PermissionId == _seededPermissionId)
                .ToListAsync();
            Assert.Empty(grants);
        }
    }

    [Fact]
    public async Task ToggleIndividualAccessAsync_NonDvmStudent_Throws()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            // User exists in AAUD but is not seeded as a current DVM student
            aaudContext.AaudUsers.Add(CreateTestUser(403, "stu403"));
            await aaudContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ToggleIndividualAccessAsync(403));
        }
    }

    #endregion

    #region UpdateStudentContact Tests

    [Fact]
    public async Task UpdateStudentContactAsync_NoPidm_ThrowsInvalidOperation()
    {
        var service = CreateService();
        var request = new UpdateStudentContactRequest { ContactPermanent = false };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStudentContactAsync(99999, request, "admin"));
    }

    [Fact]
    public async Task UpdateStudentContactAsync_CreatesNewContact()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M200", 200, "Student", "New", "V1", "12345", "student3@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var request = new UpdateStudentContactRequest
            {
                StudentInfo = new StudentInfoDto
                {
                    Address = "One Shields Avenue",
                    City = "Davis",
                    Zip = "95616",
                    HomePhone = "(530) 555-9999",
                    CellPhone = "5305551111"
                },
                ContactPermanent = true,
                LocalContact = new ContactInfoDto
                {
                    Name = "Local Person",
                    Relationship = "Friend",
                    CellPhone = "5305552222"
                },
                EmergencyContact = new ContactInfoDto
                {
                    Name = "Emergency Person",
                    Relationship = "Parent",
                    HomePhone = "(530) 555-3333",
                    Email = "parent@example.com"
                },
                PermanentContact = new ContactInfoDto()
            };

            await service.UpdateStudentContactAsync(200, request, "admin");

            var savedContact = await _sisContext.StudentContacts
                .Include(c => c.EmergencyContacts)
                .FirstOrDefaultAsync(c => c.Pidm == 12345);

            Assert.NotNull(savedContact);
            Assert.Equal("One Shields Avenue", savedContact.Address);
            Assert.Equal("Davis", savedContact.City);
            Assert.Equal("95616", savedContact.Zip);
            Assert.Equal("5305559999", savedContact.HomePhone);
            Assert.Equal("5305551111", savedContact.CellPhone);
            Assert.True(savedContact.ContactPermanent);
            Assert.Equal("admin", savedContact.UpdatedBy);
            Assert.Equal(3, savedContact.EmergencyContacts.Count);

            var local = savedContact.EmergencyContacts.First(e => e.Type == "local");
            Assert.Equal("Local Person", local.Name);
            Assert.Equal("Friend", local.Relationship);
            Assert.Equal("5305552222", local.CellPhone);

            var emergency = savedContact.EmergencyContacts.First(e => e.Type == "emergency");
            Assert.Equal("Emergency Person", emergency.Name);
            Assert.Equal("Parent", emergency.Relationship);
            Assert.Equal("5305553333", emergency.HomePhone);
            Assert.Equal("parent@example.com", emergency.Email);
        }
    }

    [Fact]
    public async Task UpdateStudentContactAsync_UpdatesExistingContact()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M201", 201, "Student", "Existing", "V1", "12346", "student4@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            // Create existing contact record
            var existingContact = new StudentContact
            {
                Pidm = 12346,
                Address = "Old Address",
                City = "Old City",
                Zip = "00000"
            };
            _sisContext.StudentContacts.Add(existingContact);
            await _sisContext.SaveChangesAsync();

            var localEc = new StudentEmergencyContact
            {
                StdContactId = existingContact.StdContactId,
                Type = "local",
                Name = "Old Name"
            };
            _sisContext.StudentEmergencyContacts.Add(localEc);
            await _sisContext.SaveChangesAsync();

            var request = new UpdateStudentContactRequest
            {
                StudentInfo = new StudentInfoDto
                {
                    Address = "New Address",
                    City = "New City",
                    Zip = "95617"
                },
                LocalContact = new ContactInfoDto
                {
                    Name = "Updated Name",
                    Relationship = "Spouse"
                },
                ContactPermanent = false,
                EmergencyContact = new ContactInfoDto(),
                PermanentContact = new ContactInfoDto()
            };

            await service.UpdateStudentContactAsync(201, request, "admin");

            var savedContact = await _sisContext.StudentContacts
                .Include(c => c.EmergencyContacts)
                .FirstOrDefaultAsync(c => c.Pidm == 12346);

            Assert.NotNull(savedContact);
            Assert.Equal("New Address", savedContact.Address);
            Assert.Equal("New City", savedContact.City);
            Assert.Equal("95617", savedContact.Zip);

            var local = savedContact.EmergencyContacts.First(e => e.Type == "local");
            Assert.Equal("Updated Name", local.Name);
            Assert.Equal("Spouse", local.Relationship);
        }
    }

    [Fact]
    public async Task UpdateStudentContactAsync_InvalidPhone_ThrowsArgumentException()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M300", 300, "Test", "Phone", "V1", "12399", "student5@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var request = new UpdateStudentContactRequest
            {
                StudentInfo = new StudentInfoDto
                {
                    Address = "789 Elm St",
                    City = "Davis",
                    Zip = "95616",
                    HomePhone = "12345" // invalid phone number
                },
                ContactPermanent = false,
                LocalContact = new ContactInfoDto(),
                EmergencyContact = new ContactInfoDto(),
                PermanentContact = new ContactInfoDto()
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => service.UpdateStudentContactAsync(300, request, "admin"));
            Assert.Contains("Student Home Phone", ex.Message);

            // Verify nothing was persisted
            var saved = await _sisContext.StudentContacts
                .FirstOrDefaultAsync(c => c.Pidm == 12399);
            Assert.Null(saved);
        }
    }

    [Fact]
    public async Task UpdateStudentContactAsync_UsesViewPidm_NotAaudUserPidm()
    {
        // Regression: AaudUsers.Pidm and VwDvmStudentsMaxTerms.IdsPidm can diverge.
        // The detail/save path must use the view PIDM (same source as list/report).
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            // Seed with DIFFERENT PIDMs: AaudUser has stale "99999", view has correct "12700"
            aaudContext.Set<VwDvmStudentsMaxTerm>().Add(new VwDvmStudentsMaxTerm
            {
                IdsMothraId = "M700",
                PersonLastName = "Divergent",
                PersonFirstName = "Student",
                StudentsClassLevel = "V1",
                IdsPidm = "12700",
                IdsMailid = "divergent@ucdavis.edu",
                StudentsTermCode = "202610"
            });
            aaudContext.AaudUsers.Add(new AaudUser
            {
                AaudUserId = 700,
                ClientId = "UCD",
                MothraId = "M700",
                LoginId = "user700",
                Pidm = "99999", // stale — different from view
                DisplayFullName = "Student Divergent",
                DisplayFirstName = "Student",
                DisplayLastName = "Divergent",
                LastName = "Divergent",
                FirstName = "Student"
            });
            await aaudContext.SaveChangesAsync();

            var request = new UpdateStudentContactRequest
            {
                StudentInfo = new StudentInfoDto
                {
                    Address = "Test Address",
                    City = "Davis",
                    Zip = "95616"
                },
                ContactPermanent = false,
                LocalContact = new ContactInfoDto(),
                EmergencyContact = new ContactInfoDto(),
                PermanentContact = new ContactInfoDto()
            };

            await service.UpdateStudentContactAsync(700, request, "admin");

            // Should save using the VIEW pidm (12700), not the stale AaudUser pidm (99999)
            var correctContact = await _sisContext.StudentContacts
                .FirstOrDefaultAsync(c => c.Pidm == 12700);
            Assert.NotNull(correctContact);
            Assert.Equal("Test Address", correctContact.Address);

            var staleContact = await _sisContext.StudentContacts
                .FirstOrDefaultAsync(c => c.Pidm == 99999);
            Assert.Null(staleContact);
        }
    }

    #endregion

    #region GetStudentContactList Tests

    [Fact]
    public async Task GetStudentContactListAsync_ReturnsStudentsFromAaudView()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M500", 500, "LastA", "StudentA", "V1", "12500", "studenta@ucdavis.edu");
            SeedDvmStudent(aaudContext, "M501", 501, "LastB", "StudentB", "V2", "12501", "studentb@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var result = await service.GetStudentContactListAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.PersonId == 500 && r.FullName == "LastA, StudentA");
            Assert.Contains(result, r => r.PersonId == 501 && r.FullName == "LastB, StudentB");
            Assert.All(result, r =>
            {
                Assert.Equal(r.PersonId.ToString(), r.RowKey);
                Assert.True(r.HasDetailRoute);
            });
        }
    }

    [Fact]
    public async Task GetStudentContactListAsync_UsesClassLevelFromView()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M502", 502, "LastC", "StudentC", "V3", "12502", "studentc@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var result = await service.GetStudentContactListAsync();

            var student = Assert.Single(result);
            Assert.Equal("V3", student.ClassLevel);
        }
    }

    [Fact]
    public async Task GetStudentContactListAsync_UsesEmailFromView()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M503", 503, "Doe", "Jane", "V1", "12503", "jdoe@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var result = await service.GetStudentContactListAsync();

            var student = Assert.Single(result);
            Assert.Equal("jdoe@ucdavis.edu", student.Email);
        }
    }

    [Fact]
    public async Task GetStudentContactListAsync_IncludesContactCompleteness()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M504", 504, "Doe", "John", "V2", "12504", "jdoe2@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            // Seed contact data
            var contact = new StudentContact
            {
                Pidm = 12504,
                Address = "One Shields Avenue",
                City = "Davis",
                Zip = "95616",
                CellPhone = "5305551234"
            };
            _sisContext.StudentContacts.Add(contact);
            await _sisContext.SaveChangesAsync();

            var localEc = new StudentEmergencyContact
            {
                StdContactId = contact.StdContactId,
                Type = "local",
                Name = "Jane Doe",
                Relationship = "Friend",
                CellPhone = "5305552222",
                Email = "jdoe@example.com"
            };
            _sisContext.StudentEmergencyContacts.Add(localEc);
            await _sisContext.SaveChangesAsync();

            var result = await service.GetStudentContactListAsync();

            var student = Assert.Single(result);
            Assert.Equal("5305551234", student.CellPhone);
            Assert.Equal(EmergencyContactService.StudentInfoFieldCount, student.StudentInfoComplete);
            Assert.Empty(student.StudentInfoMissing);
            Assert.Equal(EmergencyContactService.ContactFieldCount, student.LocalContactComplete);
        }
    }

    [Fact]
    public async Task GetStudentContactListAsync_IncludesStudentsWithoutPersonIdMapping()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            // Add view record but no AaudUser mapping — student should still appear
            aaudContext.VwDvmStudentsMaxTerms.Add(new VwDvmStudentsMaxTerm
            {
                IdsMothraId = "M999",
                PersonLastName = "Doe",
                PersonFirstName = "John",
                StudentsClassLevel = "V1",
                IdsPidm = "19999",
                IdsMailid = "nobody@ucdavis.edu",
                StudentsTermCode = "202610"
            });
            await aaudContext.SaveChangesAsync();

            var result = await service.GetStudentContactListAsync();

            var student = Assert.Single(result);
            Assert.Equal(0, student.PersonId);
            Assert.Equal("M999", student.RowKey);
            Assert.False(student.HasDetailRoute);
            Assert.Equal("Doe, John", student.FullName);
        }
    }

    #endregion

    #region GetStudentContactReport Tests

    [Fact]
    public async Task GetStudentContactReportAsync_ReturnsStudentsFromAaudView()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M600", 600, "Doe", "Jane", "V1", "12600", "jdoe3@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var result = await service.GetStudentContactReportAsync();

            var student = Assert.Single(result);
            Assert.Equal(600, student.PersonId);
            Assert.Equal("Doe, Jane", student.FullName);
            Assert.Equal("V1", student.ClassLevel);
        }
    }

    [Fact]
    public async Task GetStudentContactReportAsync_IncludesContactDetails()
    {
        var (aaudContext, service) = CreateServiceWithViewSupport();
        using (aaudContext)
        {
            SeedDvmStudent(aaudContext, "M601", 601, "Doe", "John", "V2", "12601", "jdoe4@ucdavis.edu");
            await aaudContext.SaveChangesAsync();

            var contact = new StudentContact
            {
                Pidm = 12601,
                Address = "One Shields Avenue",
                City = "Davis",
                Zip = "95616",
                HomePhone = "5305559999",
                CellPhone = "5305558888",
                ContactPermanent = true
            };
            _sisContext.StudentContacts.Add(contact);
            await _sisContext.SaveChangesAsync();

            var emergencyEc = new StudentEmergencyContact
            {
                StdContactId = contact.StdContactId,
                Type = "emergency",
                Name = "Jane Doe",
                Relationship = "Parent",
                HomePhone = "5305557777",
                Email = "jdoe@example.com"
            };
            _sisContext.StudentEmergencyContacts.Add(emergencyEc);
            await _sisContext.SaveChangesAsync();

            var result = await service.GetStudentContactReportAsync();

            var student = Assert.Single(result);
            Assert.Equal("One Shields Avenue", student.Address);
            Assert.Equal("Davis", student.City);
            Assert.Equal("95616", student.Zip);
            Assert.True(student.ContactPermanent);
            Assert.NotNull(student.EmergencyContact);
            Assert.Equal("Jane Doe", student.EmergencyContact!.Name);
        }
    }

    #endregion

    #region Helper Methods

    private EmergencyContactService CreateService()
    {
        var userHelper = CreateMockUserHelper();
        var logger = Substitute.For<ILogger<EmergencyContactService>>();
        return new EmergencyContactService(
            _sisContext, _rapsContext, _aaudContext, userHelper, logger);
    }

    private static IUserHelper CreateMockUserHelper()
    {
        var userHelper = Substitute.For<IUserHelper>();
        userHelper.GetCurrentUser().Returns(new AaudUser
        {
            AaudUserId = 1,
            ClientId = "UCD",
            MothraId = "TESTADM",
            LoginId = "testadmin",
            DisplayFullName = "Test Admin",
            DisplayFirstName = "Test",
            DisplayLastName = "Admin",
            LastName = "Admin",
            FirstName = "Test"
        });
        return userHelper;
    }

    /// <summary>
    /// Creates a service with a mock IUserHelper that grants specified permissions.
    /// No permissions granted by default (simulates a regular student).
    /// </summary>
    private EmergencyContactService CreateServiceWithPermissions(params string[] grantedPermissions)
    {
        var userHelper = CreateMockUserHelper();
        foreach (var perm in grantedPermissions)
        {
            userHelper.HasPermission(Arg.Any<RAPSContext>(), Arg.Any<AaudUser>(), perm).Returns(true);
        }
        var logger = Substitute.For<ILogger<EmergencyContactService>>();
        return new EmergencyContactService(
            _sisContext, _rapsContext, _aaudContext, userHelper, logger);
    }

    private static AaudUser CreateTestUser(int personId, string loginId)
    {
        return new AaudUser
        {
            AaudUserId = personId,
            ClientId = "UCD",
            MothraId = $"M{personId}",
            LoginId = loginId,
            DisplayFullName = $"Test User {personId}",
            DisplayFirstName = "Test",
            DisplayLastName = $"User{personId}",
            LastName = $"User{personId}",
            FirstName = "Test"
        };
    }

    /// <summary>
    /// Creates a service with an AAUDContext that supports adding VwDvmStudentsMaxTerm
    /// records (overrides keyless view mapping for InMemory testing).
    /// </summary>
    private (TestableAAUDContext aaudContext, EmergencyContactService service) CreateServiceWithViewSupport()
    {
        var aaudOptions = new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase(databaseName: "AAUD_View_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var testAaudContext = new TestableAAUDContext(aaudOptions);

        var userHelper = CreateMockUserHelper();
        var logger = Substitute.For<ILogger<EmergencyContactService>>();
        var service = new EmergencyContactService(
            _sisContext, _rapsContext, testAaudContext, userHelper, logger);
        return (testAaudContext, service);
    }

    /// <summary>
    /// Seeds a DVM student in the AAUD view and AaudUser table.
    /// </summary>
    private static void SeedDvmStudent(
        AAUDContext aaudContext, string mothraId, int personId,
        string lastName, string firstName, string classLevel,
        string pidm, string email)
    {
        aaudContext.Set<VwDvmStudentsMaxTerm>().Add(new VwDvmStudentsMaxTerm
        {
            IdsMothraId = mothraId,
            PersonLastName = lastName,
            PersonFirstName = firstName,
            StudentsClassLevel = classLevel,
            IdsPidm = pidm,
            IdsMailid = email,
            StudentsTermCode = "202610"
        });

        aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = personId,
            ClientId = "UCD",
            MothraId = mothraId,
            LoginId = $"user{personId}",
            Pidm = pidm,
            DisplayFullName = $"{firstName} {lastName}",
            DisplayFirstName = firstName,
            DisplayLastName = lastName,
            LastName = lastName,
            FirstName = firstName
        });
    }

    /// <summary>
    /// AAUDContext subclass that maps keyless views as tables with keys,
    /// enabling InMemory provider to store test data.
    /// </summary>
    private class TestableAAUDContext : AAUDContext
    {
        public TestableAAUDContext(DbContextOptions<AAUDContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VwDvmStudentsMaxTerm>(entity =>
            {
                entity.HasKey(e => e.IdsMothraId);
                entity.ToTable("VwDvmStudentsMaxTerm");
            });
        }
    }

    #endregion
}
