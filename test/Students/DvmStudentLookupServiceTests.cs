using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Students;

/// <summary>
/// Tests for DvmStudentLookupService: who counts as a current DVM student, and the identity the
/// student self-service apps read for them.
/// </summary>
public sealed class DvmStudentLookupServiceTests : IDisposable
{
    private readonly TestableAAUDContext _aaudContext;
    private readonly DvmStudentLookupService _service;

    public DvmStudentLookupServiceTests()
    {
        _aaudContext = new TestableAAUDContext(new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase($"AAUD_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
        _service = new DvmStudentLookupService(_aaudContext);
    }

    public void Dispose()
    {
        _aaudContext.Dispose();
    }

    #region LoadDvmStudentsAsync

    [Fact]
    public async Task LoadDvmStudentsAsync_MapsMothraIdsToPersonIds()
    {
        await SeedStudentAsync("STU00001", 100, pidm: "20000001");
        await SeedStudentAsync("STU00002", 101, pidm: "20000002");

        var (students, mothraToPersonId) = await _service.LoadDvmStudentsAsync();

        Assert.Equal(2, students.Count);
        Assert.Equal(100, mothraToPersonId["STU00001"]);
        Assert.Equal(101, mothraToPersonId["STU00002"]);
    }

    [Fact]
    public async Task LoadDvmStudentsAsync_StudentWithNoAaudUser_IsListedButUnmapped()
    {
        await SeedStudentAsync("STU00009", personId: null, pidm: "20000009");

        var (students, mothraToPersonId) = await _service.LoadDvmStudentsAsync();

        Assert.Single(students);
        Assert.Empty(mothraToPersonId);
    }

    [Fact]
    public async Task LoadDvmStudentsAsync_IgnoresNonStudentUsers()
    {
        await SeedStudentAsync("STU00001", 100, pidm: "20000001");
        _aaudContext.AaudUsers.Add(NewUser("FAC00001", 500));
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (students, mothraToPersonId) = await _service.LoadDvmStudentsAsync();

        Assert.Single(students);
        Assert.Single(mothraToPersonId);
    }

    #endregion

    #region GetDvmStudentAsync

    [Fact]
    public async Task GetDvmStudentAsync_ReturnsIdentityAndPidm()
    {
        await SeedStudentAsync("STU00001", 100, pidm: "20000001", lastName: "Student", firstName: "Test");

        var student = await _service.GetDvmStudentAsync(100);

        Assert.NotNull(student);
        Assert.Equal(100, student.PersonId);
        Assert.Equal("Student, Test", student.FullName);
        Assert.Equal("V1", student.ClassLevel);
        Assert.Equal(20000001, student.Pidm);
    }

    [Fact]
    public async Task GetDvmStudentAsync_NotADvmStudent_ReturnsNull()
    {
        _aaudContext.AaudUsers.Add(NewUser("FAC00001", 500));
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await _service.GetDvmStudentAsync(500));
    }

    [Fact]
    public async Task GetDvmStudentAsync_UnparseablePidm_ReturnsIdentityWithoutOne()
    {
        // The record pages hand back an empty shell rather than failing when PIDM is missing.
        await SeedStudentAsync("STU00001", 100, pidm: "");

        var student = await _service.GetDvmStudentAsync(100);

        Assert.NotNull(student);
        Assert.Null(student.Pidm);
    }

    [Fact]
    public async Task GetDvmStudentAsync_MissingClassLevel_ReadsAsEmpty()
    {
        await SeedStudentAsync("STU00001", 100, pidm: "20000001", classLevel: null);

        var student = await _service.GetDvmStudentAsync(100);

        Assert.NotNull(student);
        Assert.Equal(string.Empty, student.ClassLevel);
    }

    #endregion

    #region IsCurrentDvmStudentAsync and GetCurrentDvmPidmAsync

    [Fact]
    public async Task IsCurrentDvmStudentAsync_CurrentStudent_ReturnsTrue()
    {
        await SeedStudentAsync("STU00001", 100, pidm: "20000001");

        Assert.True(await _service.IsCurrentDvmStudentAsync(100));
    }

    [Fact]
    public async Task IsCurrentDvmStudentAsync_NotAStudent_ReturnsFalse()
    {
        _aaudContext.AaudUsers.Add(NewUser("FAC00001", 500));
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.False(await _service.IsCurrentDvmStudentAsync(500));
    }

    [Fact]
    public async Task GetCurrentDvmPidmAsync_ReturnsThePidmFromTheView()
    {
        await SeedStudentAsync("STU00001", 100, pidm: "20000001");

        Assert.Equal(20000001, await _service.GetCurrentDvmPidmAsync(100));
    }

    [Fact]
    public async Task GetCurrentDvmPidmAsync_UnknownPerson_ReturnsNull()
    {
        Assert.Null(await _service.GetCurrentDvmPidmAsync(999));
    }

    #endregion

    #region FormatEmail

    [Theory]
    [InlineData("tstudent", "tstudent@ucdavis.edu")]
    [InlineData("tstudent@ucdavis.edu", "tstudent@ucdavis.edu")]
    [InlineData("someone@example.com", "someone@example.com")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void FormatEmail_CompletesBareMailIdsOnly(string? mailId, string expected)
    {
        Assert.Equal(expected, DvmStudentLookupService.FormatEmail(mailId));
    }

    #endregion

    private async Task SeedStudentAsync(string mothraId, int? personId, string pidm,
        string lastName = "Student", string firstName = "Test", string? classLevel = "V1")
    {
        _aaudContext.Set<VwDvmStudentsMaxTerm>().Add(new VwDvmStudentsMaxTerm
        {
            IdsMothraId = mothraId,
            PersonLastName = lastName,
            PersonFirstName = firstName,
            StudentsClassLevel = classLevel,
            IdsPidm = pidm,
            IdsMailid = "student",
            StudentsTermCode = "202610"
        });

        if (personId != null)
        {
            _aaudContext.AaudUsers.Add(NewUser(mothraId, personId.Value, lastName, firstName));
        }

        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AaudUser NewUser(string mothraId, int personId, string lastName = "Person", string firstName = "Test") =>
        new()
        {
            AaudUserId = personId,
            ClientId = "UCD",
            MothraId = mothraId,
            LoginId = $"user{personId}",
            DisplayFullName = $"{firstName} {lastName}",
            DisplayFirstName = firstName,
            DisplayLastName = lastName,
            LastName = lastName,
            FirstName = firstName
        };
}
