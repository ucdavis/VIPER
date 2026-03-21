using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Viper.Areas.Students.Constants;
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
    private readonly VIPERContext _viperContext;

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

        var viperOptions = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: "VIPER_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _viperContext = new VIPERContext(viperOptions);

        // Seed the RAPS permission and role so string-based lookups work
        var permission = new TblPermission { Permission = EmergencyContactPermissions.Student };
        _rapsContext.TblPermissions.Add(permission);
        var role = new TblRole { Role = EmergencyContactPermissions.StudentRoleName };
        _rapsContext.TblRoles.Add(role);
        _rapsContext.SaveChanges();
        _seededPermissionId = permission.PermissionId;
        _seededRoleId = role.RoleId;

        // Seed a current term so StudentList.GetStudents() can resolve active class years
        _viperContext.Terms.Add(new Viper.Models.VIPER.Term
        {
            TermCode = 202610,
            AcademicYear = 2027,
            Description = "Fall 2026",
            StartDate = new DateTime(2026, 9, 21),
            EndDate = new DateTime(2026, 12, 11),
            TermType = "Q",
            CurrentTerm = true
        });
        _viperContext.SaveChanges();
    }

    public void Dispose()
    {
        _sisContext.Dispose();
        _rapsContext.Dispose();
        _aaudContext.Dispose();
        _viperContext.Dispose();
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
    public void CalculateStudentInfoCompleteness_AllEmpty_ReturnsZero()
    {
        var contact = new StudentContact();
        Assert.Equal(0, EmergencyContactService.CalculateStudentInfoCompleteness(contact));
    }

    [Fact]
    public void CalculateStudentInfoCompleteness_AddressOnly_ReturnsZero()
    {
        // Address alone is not enough; need address+city+zip for 1 point
        var contact = new StudentContact { Address = "123 Main St" };
        Assert.Equal(0, EmergencyContactService.CalculateStudentInfoCompleteness(contact));
    }

    [Fact]
    public void CalculateStudentInfoCompleteness_AddressCityZip_ReturnsOne()
    {
        var contact = new StudentContact
        {
            Address = "123 Main St",
            City = "Davis",
            Zip = "95616"
        };
        Assert.Equal(1, EmergencyContactService.CalculateStudentInfoCompleteness(contact));
    }

    [Fact]
    public void CalculateStudentInfoCompleteness_AllFields_ReturnsThree()
    {
        var contact = new StudentContact
        {
            Address = "123 Main St",
            City = "Davis",
            Zip = "95616",
            HomePhone = "5305551234",
            CellPhone = "5305555678"
        };
        Assert.Equal(3, EmergencyContactService.CalculateStudentInfoCompleteness(contact));
    }

    [Fact]
    public void CalculateStudentInfoCompleteness_OnlyPhones_ReturnsTwo()
    {
        var contact = new StudentContact
        {
            HomePhone = "5305551234",
            CellPhone = "5305555678"
        };
        Assert.Equal(2, EmergencyContactService.CalculateStudentInfoCompleteness(contact));
    }

    #endregion

    #region Contact Completeness Tests

    [Fact]
    public void CalculateContactCompleteness_NullContact_ReturnsZero()
    {
        Assert.Equal(0, EmergencyContactService.CalculateContactCompleteness(null));
    }

    [Fact]
    public void CalculateContactCompleteness_EmptyContact_ReturnsZero()
    {
        var contact = new StudentEmergencyContact { Type = "local" };
        Assert.Equal(0, EmergencyContactService.CalculateContactCompleteness(contact));
    }

    [Fact]
    public void CalculateContactCompleteness_ThreeFields_ReturnsThree()
    {
        var contact = new StudentEmergencyContact
        {
            Type = "emergency",
            Name = "Jane Doe",
            Relationship = "Mother",
            CellPhone = "5305551234"
        };
        Assert.Equal(3, EmergencyContactService.CalculateContactCompleteness(contact));
    }

    [Fact]
    public void CalculateContactCompleteness_AllFields_ReturnsSix()
    {
        var contact = new StudentEmergencyContact
        {
            Type = "local",
            Name = "John Smith",
            Relationship = "Father",
            WorkPhone = "5305551111",
            HomePhone = "5305552222",
            CellPhone = "5305553333",
            Email = "john@example.com"
        };
        Assert.Equal(6, EmergencyContactService.CalculateContactCompleteness(contact));
    }

    [Fact]
    public void CalculateContactCompleteness_WhitespaceFields_ReturnsZero()
    {
        var contact = new StudentEmergencyContact
        {
            Type = "emergency",
            Name = "  ",
            Relationship = "",
            Email = null
        };
        Assert.Equal(0, EmergencyContactService.CalculateContactCompleteness(contact));
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
        _rapsContext.TblRolePermissions.Add(new Viper.Models.RAPS.TblRolePermission
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

    #endregion

    #region ToggleAppAccess Tests

    [Fact]
    public async Task ToggleAppAccessAsync_WhenClosed_OpensAndReturnsTrue()
    {
        var service = CreateService();

        var result = await service.ToggleAppAccessAsync();

        Assert.True(result);
        Assert.True(await _rapsContext.TblRolePermissions.AnyAsync(rp =>
            rp.RoleId == _seededRoleId
            && rp.PermissionId == _seededPermissionId));
    }

    [Fact]
    public async Task ToggleAppAccessAsync_WhenOpen_ClosesAndReturnsFalse()
    {
        // Open first
        _rapsContext.TblRolePermissions.Add(new Viper.Models.RAPS.TblRolePermission
        {
            RoleId = _seededRoleId,
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.ToggleAppAccessAsync();

        Assert.False(result);
        Assert.False(await _rapsContext.TblRolePermissions.AnyAsync(rp =>
            rp.RoleId == _seededRoleId
            && rp.PermissionId == _seededPermissionId));
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
        await SeedCurrentStudentAsync(100, "Test", "Student");

        _aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = 100,
            ClientId = "UCD",
            MothraId = "M100",
            LoginId = "student1",
            DisplayFullName = "Test Student",
            DisplayFirstName = "Test",
            DisplayLastName = "Student",
            LastName = "Student",
            FirstName = "Test"
        });
        await _aaudContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.ToggleIndividualAccessAsync(100);

        Assert.True(result);
        Assert.True(await _rapsContext.TblMemberPermissions.AnyAsync(mp =>
            mp.MemberId == "M100"
            && mp.PermissionId == _seededPermissionId));
    }

    [Fact]
    public async Task ToggleIndividualAccessAsync_ExistingGrant_RemovesAndReturnsFalse()
    {
        await SeedCurrentStudentAsync(101, "Test", "Student2");

        _aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = 101,
            ClientId = "UCD",
            MothraId = "M101",
            LoginId = "student2",
            DisplayFullName = "Test Student 2",
            DisplayFirstName = "Test",
            DisplayLastName = "Student2",
            LastName = "Student2",
            FirstName = "Test"
        });
        await _aaudContext.SaveChangesAsync();

        _rapsContext.TblMemberPermissions.Add(new Viper.Models.RAPS.TblMemberPermission
        {
            MemberId = "M101",
            PermissionId = _seededPermissionId,
            Access = 1
        });
        await _rapsContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.ToggleIndividualAccessAsync(101);

        Assert.False(result);
        Assert.False(await _rapsContext.TblMemberPermissions.AnyAsync(mp =>
            mp.MemberId == "M101"
            && mp.PermissionId == _seededPermissionId));
    }

    #endregion

    #region UpdateStudentContact Tests

    [Fact]
    public async Task UpdateStudentContactAsync_NoPidm_ThrowsInvalidOperation()
    {
        var service = CreateService();
        var request = new Viper.Areas.Students.Models.UpdateStudentContactRequest { ContactPermanent = false };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStudentContactAsync(99999, request, "admin"));
    }

    [Fact]
    public async Task UpdateStudentContactAsync_CreatesNewContact()
    {
        await SeedCurrentStudentAsync(200, "New", "Student");

        // Setup AAUD user with a PIDM
        _aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = 200,
            ClientId = "UCD",
            MothraId = "M200",
            LoginId = "student3",
            Pidm = "12345",
            DisplayFullName = "New Student",
            DisplayFirstName = "New",
            DisplayLastName = "Student",
            LastName = "Student",
            FirstName = "New"
        });
        await _aaudContext.SaveChangesAsync();

        var service = CreateService();
        var request = new Viper.Areas.Students.Models.UpdateStudentContactRequest
        {
            StudentInfo = new Viper.Areas.Students.Models.StudentInfoDto
            {
                Address = "456 Oak Ave",
                City = "Davis",
                Zip = "95616",
                HomePhone = "(530) 555-9999",
                CellPhone = "5305551111"
            },
            ContactPermanent = true,
            LocalContact = new Viper.Areas.Students.Models.ContactInfoDto
            {
                Name = "Local Person",
                Relationship = "Friend",
                CellPhone = "5305552222"
            },
            EmergencyContact = new Viper.Areas.Students.Models.ContactInfoDto
            {
                Name = "Emergency Person",
                Relationship = "Parent",
                HomePhone = "(530) 555-3333",
                Email = "parent@example.com"
            },
            PermanentContact = new Viper.Areas.Students.Models.ContactInfoDto()
        };

        await service.UpdateStudentContactAsync(200, request, "admin");

        var savedContact = await _sisContext.StudentContacts
            .Include(c => c.EmergencyContacts)
            .FirstOrDefaultAsync(c => c.Pidm == 12345);

        Assert.NotNull(savedContact);
        Assert.Equal("456 Oak Ave", savedContact.Address);
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

    [Fact]
    public async Task UpdateStudentContactAsync_UpdatesExistingContact()
    {
        await SeedCurrentStudentAsync(201, "Existing", "Student");

        // Setup AAUD user
        _aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = 201,
            ClientId = "UCD",
            MothraId = "M201",
            LoginId = "student4",
            Pidm = "12346",
            DisplayFullName = "Existing Student",
            DisplayFirstName = "Existing",
            DisplayLastName = "Student",
            LastName = "Student",
            FirstName = "Existing"
        });
        await _aaudContext.SaveChangesAsync();

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

        var service = CreateService();
        var request = new Viper.Areas.Students.Models.UpdateStudentContactRequest
        {
            StudentInfo = new Viper.Areas.Students.Models.StudentInfoDto
            {
                Address = "New Address",
                City = "New City",
                Zip = "95617"
            },
            LocalContact = new Viper.Areas.Students.Models.ContactInfoDto
            {
                Name = "Updated Name",
                Relationship = "Spouse"
            },
            ContactPermanent = false,
            EmergencyContact = new Viper.Areas.Students.Models.ContactInfoDto(),
            PermanentContact = new Viper.Areas.Students.Models.ContactInfoDto()
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

    [Fact]
    public async Task UpdateStudentContactAsync_InvalidPhone_ThrowsArgumentException()
    {
        await SeedCurrentStudentAsync(300, "Phone", "Test");

        _aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = 300,
            ClientId = "UCD",
            MothraId = "M300",
            LoginId = "student5",
            Pidm = "12399",
            DisplayFullName = "Phone Test Student",
            DisplayFirstName = "Phone",
            DisplayLastName = "Test",
            LastName = "Test",
            FirstName = "Phone"
        });
        await _aaudContext.SaveChangesAsync();

        var service = CreateService();
        var request = new Viper.Areas.Students.Models.UpdateStudentContactRequest
        {
            StudentInfo = new Viper.Areas.Students.Models.StudentInfoDto
            {
                Address = "789 Elm St",
                City = "Davis",
                Zip = "95616",
                HomePhone = "12345" // invalid phone number
            },
            ContactPermanent = false,
            LocalContact = new Viper.Areas.Students.Models.ContactInfoDto(),
            EmergencyContact = new Viper.Areas.Students.Models.ContactInfoDto(),
            PermanentContact = new Viper.Areas.Students.Models.ContactInfoDto()
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateStudentContactAsync(300, request, "admin"));
        Assert.Contains("Student Home Phone", ex.Message);

        // Verify nothing was persisted
        var saved = await _sisContext.StudentContacts
            .FirstOrDefaultAsync(c => c.Pidm == 12399);
        Assert.Null(saved);
    }

    #endregion

    #region Helper Methods

    private EmergencyContactService CreateService()
    {
        var userHelper = Substitute.For<IUserHelper>();
        return new EmergencyContactService(
            _sisContext, _rapsContext, _viperContext, _aaudContext, userHelper);
    }

    /// <summary>
    /// Seeds the VIPER Person + active StudentClassYear required for
    /// IsCurrentDvmStudentAsync to pass. Term is seeded in the constructor.
    /// </summary>
    private async Task SeedCurrentStudentAsync(int personId, string firstName, string lastName)
    {
        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = personId,
            ClientId = "UCD",
            MothraId = $"M{personId}",
            FirstName = firstName,
            LastName = lastName,
            FullName = $"{lastName}, {firstName}",
            Current = 1
        });

        _viperContext.StudentClassYears.Add(new Viper.Models.Students.StudentClassYear
        {
            PersonId = personId,
            ClassYear = 2027,
            Active = true,
            Added = DateTime.Now
        });

        await _viperContext.SaveChangesAsync();
    }

    #endregion
}
