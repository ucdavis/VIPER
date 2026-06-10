using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.Courses;

namespace Viper.test.Students;

/// <summary>
/// Regression tests for the StudentGroupService photo-gallery queries.
///
/// These run the AAUD query against the *relational* SQLite provider, NOT the InMemory
/// provider. That distinction is the whole point: a refactor once projected each row to
/// a record and then composed OrderBy/Where on top of that projection. It compiled, but
/// EF threw "The LINQ expression ... could not be translated" at execution, the service
/// swallowed it into an empty list, and the gallery silently showed no students. The
/// InMemory provider executes LINQ in memory and never attempts SQL translation, so it
/// cannot catch this class of bug; SQLite goes through the same relational translator as
/// SQL Server and does.
/// </summary>
public sealed class StudentGroupServiceQueryTests : IDisposable
{
    private const string Term = "202602";

    private readonly SqliteConnection _aaudConnection;
    private readonly AAUDContext _aaudContext;
    private readonly SISContext _sisContext;
    private readonly CoursesContext _coursesContext;
    private readonly VIPERContext _viperContext;
    private readonly StudentGroupService _service;

    /// <summary>
    /// A trimmed AAUDContext that maps only the four entities the gallery query touches.
    /// The full AAUD warehouse model has computed columns and views that SQLite
    /// EnsureCreated cannot build, so we ignore everything else and create just these.
    /// Column names differ from production (no HasColumnName mapping), which is irrelevant
    /// here: EF uses the same model for both schema creation and query translation.
    /// </summary>
    private sealed class TestAaudContext(DbContextOptions<AAUDContext> options) : AAUDContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type> { typeof(Id), typeof(Person), typeof(Student), typeof(Studentgrp) };
            foreach (var clrType in modelBuilder.Model.GetEntityTypes()
                         .Select(e => e.ClrType)
                         .Where(t => !keep.Contains(t))
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(clrType);
            }

            modelBuilder.Entity<Id>().HasKey(e => e.IdsPKey);
            modelBuilder.Entity<Person>().HasKey(e => e.PersonPKey);
            modelBuilder.Entity<Student>().HasKey(e => e.StudentsPKey);
            modelBuilder.Entity<Studentgrp>().HasKey(e => e.StudentgrpPidm);
        }
    }

    public StudentGroupServiceQueryTests()
    {
        _aaudConnection = new SqliteConnection("DataSource=:memory:");
        _aaudConnection.Open();
        _aaudContext = new TestAaudContext(
            new DbContextOptionsBuilder<AAUDContext>().UseSqlite(_aaudConnection).Options);
        _aaudContext.Database.EnsureCreated();

        _sisContext = new SISContext(InMemory<SISContext>("SIS"));
        _coursesContext = new CoursesContext(InMemory<CoursesContext>("Courses"));
        _viperContext = new VIPERContext(InMemory<VIPERContext>("VIPER"));

        _viperContext.Terms.Add(new Viper.Models.VIPER.Term
        {
            TermCode = int.Parse(Term),
            CurrentTerm = true,
            Description = "Current",
            TermType = "Q"
        });
        _viperContext.SaveChanges();

        var photoService = Substitute.For<IPhotoService>();
        photoService.GetDefaultPhotoUrl().Returns("default.jpg");
        photoService.GetStudentPhotoUrlsBatchAsync(Arg.Any<IEnumerable<string>>())
            .Returns(Task.FromResult(new Dictionary<string, string>()));

        _service = new StudentGroupService(
            _aaudContext,
            _sisContext,
            _coursesContext,
            photoService,
            new TermCodeService(_viperContext),
            Substitute.For<ILogger<StudentGroupService>>());
    }

    private static DbContextOptions<T> InMemory<T>(string prefix) where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(prefix + "_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private void SeedStudent(string pKey, string pidm, string lastName, string firstName, string classLevel,
        string? eighths = null, string? twentieths = null, string? team = null, string? v3Specialty = null)
    {
        _aaudContext.People.Add(new Person
        {
            PersonPKey = pKey,
            PersonTermCode = Term,
            PersonClientid = "C",
            PersonLastName = lastName,
            PersonFirstName = firstName
        });
        _aaudContext.Ids.Add(new Id
        {
            IdsPKey = pKey,
            IdsTermCode = Term,
            IdsClientid = "C",
            IdsPidm = pidm,
            IdsMailid = pKey + "@example.com",
            IdsIamId = "IAM" + pKey
        });
        _aaudContext.Students.Add(new Student
        {
            StudentsPKey = pKey,
            StudentsTermCode = Term,
            StudentsClientid = "C",
            StudentsMajorCode1 = "VMD",
            StudentsDegreeCode1 = "DVM",
            StudentsCollCode1 = "VM",
            StudentsLevelCode1 = "4",
            StudentsClassLevel = classLevel
        });
        if (eighths != null || twentieths != null || team != null || v3Specialty != null)
        {
            _aaudContext.Studentgrps.Add(new Studentgrp
            {
                StudentgrpPidm = pidm,
                StudentgrpGrp = eighths ?? string.Empty,
                Studentgrp20 = twentieths,
                StudentgrpTeamno = team,
                StudentgrpV3grp = v3Specialty
            });
        }
        _aaudContext.SaveChanges();
    }

    [Fact]
    public async Task GetStudentsByClassLevelAsync_TranslatesAndReturnsClassOrdered()
    {
        SeedStudent("P2", "PIDM2", "Young", "Amy", "V3");
        SeedStudent("P1", "PIDM1", "Adams", "Bob", "V3");
        SeedStudent("P3", "PIDM3", "Brown", "Cara", "V4"); // other class level, must be excluded

        var result = await _service.GetStudentsByClassLevelAsync("V3");

        Assert.Equal(2, result.Count);
        Assert.Equal("Adams", result[0].LastName); // ordered by last name in the database
        Assert.Equal("Young", result[1].LastName);
    }

    [Fact]
    public async Task GetStudentsByGroupAsync_TranslatesAndFiltersByGroup()
    {
        SeedStudent("P1", "PIDM1", "Adams", "Bob", "V3", eighths: "1A1");
        SeedStudent("P2", "PIDM2", "Young", "Amy", "V3", eighths: "2B2");

        var result = await _service.GetStudentsByGroupAsync("eighths", "1A1", "V3");

        Assert.Single(result);
        Assert.Equal("Adams", result[0].LastName);
    }

    [Fact]
    public async Task GetStudentsByCourseAsync_TranslatesAndReturnsEnrolled()
    {
        SeedStudent("P1", "PIDM1", "Adams", "Bob", "V3");
        SeedStudent("P2", "PIDM2", "Young", "Amy", "V3"); // not enrolled in the course
        _coursesContext.Rosters.Add(new Roster
        {
            RosterPkey = "R1",
            RosterTermCode = Term,
            RosterCrn = "12345",
            RosterEnrollStatus = "RE",
            RosterPidm = "PIDM1"
        });
        await _coursesContext.SaveChangesAsync();

        var result = await _service.GetStudentsByCourseAsync(Term, "12345");

        Assert.Single(result);
        Assert.Equal("Adams", result[0].LastName);
    }

    public void Dispose()
    {
        _aaudContext.Dispose();
        _sisContext.Dispose();
        _coursesContext.Dispose();
        _viperContext.Dispose();
        _aaudConnection.Dispose();
    }
}
