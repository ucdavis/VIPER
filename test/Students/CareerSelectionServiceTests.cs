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

namespace Viper.test.Students;

/// <summary>
/// Tests for CareerSelectionService: who may see which students, the completeness and value
/// mapping behind the roster and report, and the rules a save is held to.
/// </summary>
public sealed class CareerSelectionServiceTests : IDisposable
{
    private const string MentorMothraId = "FAC00001";
    private const string OtherMentorMothraId = "FAC00002";

    private readonly TestableAAUDContext _aaudContext;
    private readonly RAPSContext _rapsContext;
    private readonly VIPERContext _viperContext;
    private readonly IUserHelper _userHelper;
    private readonly CareerSelectionService _service;

    public CareerSelectionServiceTests()
    {
        _aaudContext = new TestableAAUDContext(InMemory<AAUDContext>("AAUD"));
        _rapsContext = new RAPSContext(InMemory<RAPSContext>("RAPS"));
        _viperContext = new VIPERContext(InMemory<VIPERContext>("VIPER"));
        _userHelper = Substitute.For<IUserHelper>();
        _service = new CareerSelectionService(
            _rapsContext, _aaudContext, _userHelper, _viperContext,
            Substitute.For<ILogger<CareerSelectionService>>(),
            new StudentAppAccessService(_rapsContext, _userHelper),
            new DvmStudentLookupService(_aaudContext));
    }

    public void Dispose()
    {
        _aaudContext.Dispose();
        _rapsContext.Dispose();
        _viperContext.Dispose();
    }

    #region ResolveScope

    [Fact]
    public void ResolveScope_Admin_SeesEveryStudent()
    {
        var user = Grant(CareerSelectionPermissions.Admin);

        Assert.Equal(CareerSelectionScope.All, _service.ResolveScope(user));
    }

    [Fact]
    public void ResolveScope_ReadOnly_SeesEveryStudent()
    {
        var user = Grant(CareerSelectionPermissions.ReadOnly);

        Assert.Equal(CareerSelectionScope.All, _service.ResolveScope(user));
    }

    [Fact]
    public void ResolveScope_Faculty_SeesTheirMentees()
    {
        var user = Grant(CareerSelectionPermissions.Faculty);

        Assert.Equal(CareerSelectionScope.Mentored, _service.ResolveScope(user));
    }

    [Fact]
    public void ResolveScope_AdminWhoIsAlsoFaculty_StillSeesEveryStudent()
    {
        // Admin outranks Faculty, so holding both must not narrow the roster to their mentees.
        var user = Grant(CareerSelectionPermissions.Admin, CareerSelectionPermissions.Faculty);

        Assert.Equal(CareerSelectionScope.All, _service.ResolveScope(user));
    }

    [Theory]
    [InlineData(CareerSelectionPermissions.Student)]
    [InlineData(CareerSelectionPermissions.ViewOwn)]
    public void ResolveScope_Student_SeesOnlyTheirOwn(string permission)
    {
        var user = Grant(permission);

        Assert.Equal(CareerSelectionScope.Own, _service.ResolveScope(user));
    }

    [Fact]
    public void ResolveScope_NoPermissions_SeesNothing()
    {
        var user = Grant();

        Assert.Equal(CareerSelectionScope.None, _service.ResolveScope(user));
    }

    #endregion

    #region IsMentorOfAsync

    [Fact]
    public async Task IsMentorOfAsync_RecordedMentor_ReturnsTrue()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = MentorMothraId);

        Assert.True(await _service.IsMentorOfAsync(MentorMothraId, 100));
    }

    [Fact]
    public async Task IsMentorOfAsync_SomeoneElsesMentee_ReturnsFalse()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = OtherMentorMothraId);

        Assert.False(await _service.IsMentorOfAsync(MentorMothraId, 100));
    }

    [Fact]
    public async Task IsMentorOfAsync_StudentWithNoRecord_ReturnsFalse()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");

        Assert.False(await _service.IsMentorOfAsync(MentorMothraId, 100));
    }

    [Fact]
    public async Task IsMentorOfAsync_BlankMentor_ReturnsFalse()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = null);

        Assert.False(await _service.IsMentorOfAsync("  ", 100));
    }

    #endregion

    #region GetStudentCareerListAsync

    [Fact]
    public async Task GetStudentCareerListAsync_NoMentor_ListsEveryStudent()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001", lastName: "Beta");
        await SeedStudentAsync(101, "STU00002", pidm: "20000002", lastName: "Alpha");
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = MentorMothraId);

        var result = await _service.GetStudentCareerListAsync(StudentListAccess.AllStudents);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.PersonId == 101 && r.FullName == "Alpha, Test");
    }

    [Fact]
    public async Task GetStudentCareerListAsync_Mentor_ListsOnlyTheirMentees()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedStudentAsync(101, "STU00002", pidm: "20000002");
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = MentorMothraId);
        await SeedSelectionAsync("20000002", s => s.FacultyMothraId = OtherMentorMothraId);

        var result = await _service.GetStudentCareerListAsync(StudentListAccess.MentoredBy(MentorMothraId));

        var student = Assert.Single(result);
        Assert.Equal(100, student.PersonId);
    }

    [Fact]
    public async Task GetStudentCareerListAsync_FlagsAnsweredFields()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001", mailId: "tstudent");
        await SeedOptionsAsync();
        await SeedSelectionAsync("20000001", s =>
        {
            s.Career = 1;
            s.FirstSpecies = 1;
            s.PostGrad = 1;
            s.ShortTermStatement = "Internship";
        });

        var student = Assert.Single(await _service.GetStudentCareerListAsync(StudentListAccess.AllStudents));

        Assert.True(student.DirectionCompleted);
        Assert.True(student.PrimaryFocusCompleted);
        Assert.True(student.PostGradCompleted);
        Assert.True(student.ShortTermPlansCompleted);
        Assert.False(student.SecondaryFocusCompleted);
        Assert.False(student.LongTermPlansCompleted);
        Assert.Equal("tstudent@ucdavis.edu", student.Email);
    }

    [Fact]
    public async Task GetStudentCareerListAsync_CatchAllWithoutItsText_IsNotComplete()
    {
        // "Other" only counts as answered once the free text says what it is.
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedOptionsAsync();
        await SeedSelectionAsync("20000001", s =>
        {
            s.Career = 2;
            s.CareerOther = null;
            s.FirstSpecies = 2;
            s.FirstSpeciesOther = "Camelid";
        });

        var student = Assert.Single(await _service.GetStudentCareerListAsync(StudentListAccess.AllStudents));

        Assert.False(student.DirectionCompleted);
        Assert.True(student.PrimaryFocusCompleted);
    }

    [Fact]
    public async Task GetStudentCareerListAsync_ResolvesMentorIdentity()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedMentorAsync(500, MentorMothraId, "Vet", "Ann");
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = MentorMothraId);

        var student = Assert.Single(await _service.GetStudentCareerListAsync(StudentListAccess.AllStudents));

        Assert.Equal("Vet, Ann", student.MentorName);
    }

    [Fact]
    public async Task GetStudentCareerListAsync_StudentWithNoAaudUser_IsListedWithoutARoute()
    {
        // A student missing from AaudUser has no PersonId to route on, but still belongs on the roster.
        _aaudContext.Set<VwDvmStudentsMaxTerm>().Add(new VwDvmStudentsMaxTerm
        {
            IdsMothraId = "STU00009",
            PersonLastName = "Unmapped",
            PersonFirstName = "Test",
            StudentsClassLevel = "V1",
            IdsPidm = "20000009",
            IdsMailid = "unmapped",
            StudentsTermCode = "202610"
        });
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var student = Assert.Single(await _service.GetStudentCareerListAsync(StudentListAccess.AllStudents));

        Assert.Equal(0, student.PersonId);
        Assert.False(student.HasDetailRoute);
        Assert.Equal("STU00009", student.RowKey);
    }

    [Fact]
    public async Task GetStudentCareerListAsync_OrdersByLastThenFirstName()
    {
        // Seeded out of order. "de Luca" before "Zeta" shows the sort ignores case, where an
        // ordinal sort would put every capitalized name first.
        await SeedStudentAsync(100, "STU00001", pidm: "20000001", lastName: "Zeta", firstName: "Ann");
        await SeedStudentAsync(101, "STU00002", pidm: "20000002", lastName: "Alpha", firstName: "Zed");
        await SeedStudentAsync(102, "STU00003", pidm: "20000003", lastName: "de Luca", firstName: "Ann");
        await SeedStudentAsync(103, "STU00004", pidm: "20000004", lastName: "Alpha", firstName: "Amy");

        var result = await _service.GetStudentCareerListAsync(StudentListAccess.AllStudents);

        Assert.Equal([103, 101, 102, 100], result.Select(r => r.PersonId));
    }

    [Fact]
    public async Task GetStudentCareerListAsync_SameName_OrdersByMothraId()
    {
        // Seeded with the higher MothraId first, so a pass-through of insertion order would fail.
        await SeedStudentAsync(101, "STU00002", pidm: "20000002", lastName: "Smith", firstName: "Sam");
        await SeedStudentAsync(100, "STU00001", pidm: "20000001", lastName: "Smith", firstName: "Sam");

        var result = await _service.GetStudentCareerListAsync(StudentListAccess.AllStudents);

        // PersonId 100 holds STU00001.
        Assert.Equal([100, 101], result.Select(r => r.PersonId));
    }

    #endregion

    #region GetStudentCareerReportAsync

    [Fact]
    public async Task GetStudentCareerReportAsync_ReportsSelectedLabels()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedOptionsAsync();
        await SeedSelectionAsync("20000001", s =>
        {
            s.Career = 1;
            s.FirstSpecies = 1;
            s.PostGrad = 1;
            s.ShortTermStatement = "Internship";
            s.LongTermStatement = "Practice ownership";
        });

        var student = Assert.Single(await _service.GetStudentCareerReportAsync(StudentListAccess.AllStudents));

        Assert.Equal("Academia", student.Direction);
        Assert.Equal("Equine", student.PrimaryFocus);
        Assert.Equal("Residency", student.PostGrad);
        Assert.Equal("Internship", student.ShortTermPlans);
        Assert.Equal("Practice ownership", student.LongTermPlans);
    }

    [Fact]
    public async Task GetStudentCareerReportAsync_CatchAllReportsItsFreeText()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedOptionsAsync();
        await SeedSelectionAsync("20000001", s =>
        {
            s.Career = 2;
            s.CareerOther = "Wildlife rehabilitation";
        });

        var student = Assert.Single(await _service.GetStudentCareerReportAsync(StudentListAccess.AllStudents));

        Assert.Equal("Wildlife rehabilitation", student.Direction);
    }

    [Fact]
    public async Task GetStudentCareerReportAsync_Mentor_ReportsOnlyTheirMentees()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedStudentAsync(101, "STU00002", pidm: "20000002");
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = MentorMothraId);
        await SeedSelectionAsync("20000002", s => s.FacultyMothraId = OtherMentorMothraId);

        var student = Assert.Single(await _service.GetStudentCareerReportAsync(StudentListAccess.MentoredBy(MentorMothraId)));

        Assert.Equal(100, student.PersonId);
    }

    [Fact]
    public async Task GetStudentCareerReportAsync_OrdersByLastThenFirstName()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001", lastName: "Beta", firstName: "Ann");
        await SeedStudentAsync(101, "STU00002", pidm: "20000002", lastName: "Alpha", firstName: "Zed");
        await SeedStudentAsync(102, "STU00003", pidm: "20000003", lastName: "Alpha", firstName: "Amy");

        var result = await _service.GetStudentCareerReportAsync(StudentListAccess.AllStudents);

        Assert.Equal([102, 101, 100], result.Select(r => r.PersonId));
    }

    #endregion

    #region GetStudentCareerDetailAsync

    [Fact]
    public async Task GetStudentCareerDetailAsync_UnknownStudent_ReturnsNull()
    {
        Assert.Null(await _service.GetStudentCareerDetailAsync(999, canEdit: true, canViewStudentList: true));
    }

    [Fact]
    public async Task GetStudentCareerDetailAsync_NoRecordYet_ReturnsEmptyShell()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");

        var detail = await _service.GetStudentCareerDetailAsync(100, canEdit: true, canViewStudentList: false);

        Assert.NotNull(detail);
        Assert.Equal("Student, Test", detail.FullName);
        Assert.True(detail.CanEdit);
        Assert.Null(detail.StudentInfo.Direction);
    }

    [Fact]
    public async Task GetStudentCareerDetailAsync_ReturnsStoredSelectionAndMentor()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedMentorAsync(500, MentorMothraId, "Vet", "Ann");
        await SeedOptionsAsync();
        await SeedSelectionAsync("20000001", s =>
        {
            s.Career = 1;
            s.ShortTermStatement = "Internship";
            s.FacultyMothraId = MentorMothraId;
        });

        var detail = await _service.GetStudentCareerDetailAsync(100, canEdit: false, canViewStudentList: true);

        Assert.NotNull(detail);
        Assert.Equal("Academia", detail.StudentInfo.Direction?.Label);
        Assert.Equal(1, detail.StudentInfo.Direction?.Value);
        Assert.Equal("Internship", detail.StudentInfo.ShortTermPlans);
        Assert.Equal(500, detail.StudentInfo.MentorId);
        Assert.Equal("Vet, Ann", detail.StudentInfo.MentorName);
    }

    #endregion

    #region UpdateStudentCareerSelectionAsync

    [Fact]
    public async Task UpdateStudentCareerSelectionAsync_NotACurrentStudent_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStudentCareerSelectionAsync(999, new StudentCareerInfoDto(), isAdmin: true));
    }

    [Fact]
    public async Task UpdateStudentCareerSelectionAsync_CreatesTheFirstRecord()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedOptionsAsync();

        var errors = await _service.UpdateStudentCareerSelectionAsync(100, new StudentCareerInfoDto
        {
            Direction = new CareerDropdownOption { Label = "Academia", Value = 1 },
            ShortTermPlans = "Internship",
        }, isAdmin: false);

        Assert.Empty(errors);
        var saved = await _viperContext.CareerSelections.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, saved.Career);
        Assert.Equal("Internship", saved.ShortTermStatement);
    }

    [Fact]
    public async Task UpdateStudentCareerSelectionAsync_UnknownOption_ReturnsAValidationError()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedOptionsAsync();

        var errors = await _service.UpdateStudentCareerSelectionAsync(100, new StudentCareerInfoDto
        {
            Direction = new CareerDropdownOption { Label = "Made up", Value = 99 },
        }, isAdmin: true);

        Assert.NotEmpty(errors);
        Assert.Empty(await _viperContext.CareerSelections.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateStudentCareerSelectionAsync_StudentSave_LeavesTheMentorAlone()
    {
        // The mentor is admin-managed and read-only to the student.
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedMentorAsync(500, MentorMothraId, "Vet", "Ann");
        await SeedOptionsAsync();
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = MentorMothraId);

        await _service.UpdateStudentCareerSelectionAsync(100, new StudentCareerInfoDto
        {
            ShortTermPlans = "Internship",
            MentorId = null,
        }, isAdmin: false);

        var saved = await _viperContext.CareerSelections.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(MentorMothraId, saved.FacultyMothraId);
    }

    [Fact]
    public async Task UpdateStudentCareerSelectionAsync_AdminSave_ClearsTheMentor()
    {
        await SeedStudentAsync(100, "STU00001", pidm: "20000001");
        await SeedMentorAsync(500, MentorMothraId, "Vet", "Ann");
        await SeedOptionsAsync();
        await SeedSelectionAsync("20000001", s => s.FacultyMothraId = MentorMothraId);

        await _service.UpdateStudentCareerSelectionAsync(100, new StudentCareerInfoDto { MentorId = null }, isAdmin: true);

        var saved = await _viperContext.CareerSelections.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(saved.FacultyMothraId);
    }

    #endregion

    #region CanEdit

    [Fact]
    public async Task CanEdit_Admin_CanEditAnyRecord()
    {
        var admin = await SeedStudentAsync(1, "ADM00001", pidm: "10000001", loginId: "admin");
        _userHelper.HasPermission(_rapsContext, Arg.Is<AaudUser>(u => u.LoginId == admin.LoginId),
            CareerSelectionPermissions.Admin).Returns(true);

        Assert.True(_service.CanEdit(100, admin));
    }

    [Fact]
    public async Task CanEdit_ReadOnly_CannotEdit()
    {
        var readOnly = await SeedStudentAsync(2, "RO000001", pidm: "10000002", loginId: "readonly");
        _userHelper.HasPermission(_rapsContext, Arg.Any<AaudUser>(), CareerSelectionPermissions.ReadOnly).Returns(true);

        Assert.False(_service.CanEdit(100, readOnly));
    }

    [Fact]
    public async Task CanEdit_StudentWhileAppOpen_CanEditOwnRecordOnly()
    {
        // Holding the Student permission is what "the app is open" means for a student.
        var student = await SeedStudentAsync(100, "STU00001", pidm: "20000001", loginId: "student");
        _userHelper.HasPermission(_rapsContext, Arg.Any<AaudUser>(), CareerSelectionPermissions.Student).Returns(true);

        Assert.True(_service.CanEdit(100, student));
        Assert.False(_service.CanEdit(999, student));
    }

    [Fact]
    public async Task CanEdit_StudentWithoutThePermission_CannotEdit()
    {
        var student = await SeedStudentAsync(100, "STU00001", pidm: "20000001", loginId: "student");

        Assert.False(_service.CanEdit(100, student));
    }

    #endregion

    #region Helpers

    private static DbContextOptions<T> InMemory<T>(string name) where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase($"{name}_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    /// <summary>Signs a user in and grants them the named career selection permissions.</summary>
    private AaudUser Grant(params string[] permissions)
    {
        var user = new AaudUser
        {
            AaudUserId = 1,
            ClientId = "UCD",
            MothraId = MentorMothraId,
            LoginId = "tester",
            DisplayFullName = "Test Tester",
            DisplayFirstName = "Test",
            DisplayLastName = "Tester",
            LastName = "Tester",
            FirstName = "Test"
        };
        foreach (var permission in permissions)
        {
            _userHelper.HasPermission(_rapsContext, user, permission).Returns(true);
        }
        return user;
    }

    /// <summary>Adds a DVM student to the students view and to AaudUser. Call SaveChanges after.</summary>
    private async Task<AaudUser> SeedStudentAsync(int personId, string mothraId, string pidm,
        string lastName = "Student", string mailId = "student", string loginId = "student",
        string firstName = "Test")
    {
        _aaudContext.Set<VwDvmStudentsMaxTerm>().Add(new VwDvmStudentsMaxTerm
        {
            IdsMothraId = mothraId,
            PersonLastName = lastName,
            PersonFirstName = firstName,
            StudentsClassLevel = "V1",
            IdsPidm = pidm,
            IdsMailid = mailId,
            StudentsTermCode = "202610"
        });

        var user = new AaudUser
        {
            AaudUserId = personId,
            ClientId = "UCD",
            MothraId = mothraId,
            LoginId = loginId,
            Pidm = pidm,
            DisplayFullName = $"{firstName} {lastName}",
            DisplayFirstName = firstName,
            DisplayLastName = lastName,
            LastName = lastName,
            FirstName = firstName
        };
        _aaudContext.AaudUsers.Add(user);
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private async Task SeedMentorAsync(int personId, string mothraId, string lastName, string firstName)
    {
        _aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = personId,
            ClientId = "UCD",
            MothraId = mothraId,
            LoginId = $"mentor{personId}",
            IamId = $"IAM{personId}",
            DisplayFullName = $"{firstName} {lastName}",
            DisplayFirstName = firstName,
            DisplayLastName = lastName,
            LastName = lastName,
            FirstName = firstName
        });
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Option id 1 is an ordinary choice in each list; id 2 is the catch-all.</summary>
    private async Task SeedOptionsAsync()
    {
        _viperContext.CareerOptions.AddRange(
            new CareerOption { CareerOptionId = 1, Career = "Academia" },
            new CareerOption { CareerOptionId = 2, Career = "Other", IsOther = true });
        _viperContext.SpeciesOptions.AddRange(
            new SpeciesOption { SpeciesOptionId = 1, Species = "Equine" },
            new SpeciesOption { SpeciesOptionId = 2, Species = "Other", IsOther = true });
        _viperContext.PostGradOptions.AddRange(
            new PostGradOption { PostGradOptionId = 1, PostGrad = "Residency" },
            new PostGradOption { PostGradOptionId = 2, PostGrad = "Other", IsOther = true });
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    // Takes the PIDM in the view's string form, the same literal the matching student is seeded
    // with, and converts it the way the service does.
    private async Task SeedSelectionAsync(string pidm, Action<CareerSelection> customize)
    {
        var selection = new CareerSelection { Pidm = int.Parse(pidm), DateAdded = DateTime.Now };
        customize(selection);
        _viperContext.CareerSelections.Add(selection);
        await _viperContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion
}
