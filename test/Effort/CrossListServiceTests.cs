using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.Models.Courses;
using static Viper.Areas.Effort.Services.CrossListService;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for cross-list matching: family grouping, parent selection, and child effort.
/// </summary>
public sealed class CrossListServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly CoursesContext _coursesContext;
    private readonly CrossListService _service;

    private const int TestTermCode = 202410;
    private const int PriorTermCode = 202403;

    private static readonly IReadOnlySet<int> NoParents = new HashSet<int>();

    public CrossListServiceTests()
    {
        _context = new EffortDbContext(new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
        _coursesContext = new CoursesContext(new DbContextOptionsBuilder<CoursesContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _service = new CrossListService(_context, _coursesContext,
            Substitute.For<IEffortAuditService>(), Substitute.For<ILogger<CrossListService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
        _coursesContext.Dispose();
    }

    private static CrossListCourse Course(int id, string subj, string crse, int enrollment, string seq = "001") =>
        new(id, $"1000{id}", subj, crse, seq, enrollment);

    private static List<BannerCrossList> Banner(string group, params CrossListCourse[] courses) =>
        courses.Select(c => new BannerCrossList(c.Crn, group, c.CourseCode)).ToList();

    #region BuildLinks

    [Fact]
    public void BuildLinks_PrefersSvmSubject_OverHigherEnrollment()
    {
        var apc = Course(1, "APC", "100", 11);
        var npb = Course(2, "NPB", "123", 29);

        var link = Assert.Single(BuildLinks([apc, npb], Banner("A0", apc, npb), [], NoParents));

        Assert.Equal(apc, link.Parent);
        Assert.Equal(npb, link.Child);
        Assert.Equal(CrossListType, link.RelationshipType);
        Assert.Equal(ReasonSvmSubject, link.Reason);
    }

    [Fact]
    public void BuildLinks_UsesMostRecentPriorTermParent_OverSvmSubject()
    {
        var epi = Course(1, "EPI", "277", 3);
        var phr = Course(2, "PHR", "277", 5);
        List<PriorCrossList> prior =
        [
            new(202203, "PHR 277", "EPI 277"),
            new(PriorTermCode, "EPI 277", "PHR 277")
        ];

        var link = Assert.Single(BuildLinks([epi, phr], Banner("K5", epi, phr), prior, NoParents));

        Assert.Equal(epi, link.Parent);
        Assert.Equal(ReasonPriorTerm, link.Reason);
        Assert.Equal(PriorTermCode, link.PriorTermCode);
    }

    [Fact]
    public void BuildLinks_FallsBackToHighestEnrollment_WhenNoSvmSubjectOrHistory()
    {
        var ent = Course(1, "ENT", "002", 12);
        var eve = Course(2, "EVE", "002", 16);

        var link = Assert.Single(BuildLinks([ent, eve], Banner("E0", ent, eve), [], NoParents));

        Assert.Equal(eve, link.Parent);
        Assert.Equal(ReasonEnrollment, link.Reason);
    }

    [Fact]
    public void BuildLinks_KeepsExistingParent_OverEveryOtherRule()
    {
        var apc = Course(1, "APC", "100", 11);
        var npb = Course(2, "NPB", "123", 29);

        var link = Assert.Single(BuildLinks([apc, npb], Banner("A0", apc, npb), [], new HashSet<int> { npb.Id }));

        Assert.Equal(npb, link.Parent);
        Assert.Equal(ReasonExistingLink, link.Reason);
    }

    [Fact]
    public void BuildLinks_MergesSectionGroups_UnderLowestSectionOfParentCourse()
    {
        // Banner groups each section pair separately; staff link all of them to APC 100-A01.
        var apc1 = Course(1, "APC", "100", 11, "A01");
        var apc2 = Course(2, "APC", "100", 15, "A02");
        var npb1 = Course(3, "NPB", "123", 29, "A01");
        var npb2 = Course(4, "NPB", "123", 24, "A02");
        var banner = Banner("A0", apc1, npb1).Concat(Banner("A1", apc2, npb2)).ToList();

        var links = BuildLinks([npb2, apc2, npb1, apc1], banner, [], NoParents);

        Assert.All(links, l => Assert.Equal(apc1, l.Parent));
        Assert.Equal(
            [(apc2, SectionType), (npb1, CrossListType), (npb2, CrossListType)],
            links.Select(l => (l.Child, l.RelationshipType)));
    }

    [Fact]
    public void BuildLinks_SkipsFamily_WhenPartnerCourseIsNotInTerm()
    {
        var apc = Course(1, "APC", "100", 11);
        var npb = Course(2, "NPB", "123", 29);

        Assert.Empty(BuildLinks([apc], Banner("A0", apc, npb), [], NoParents));
    }

    [Fact]
    public void BuildLinks_DoesNotLinkUnitVariantsOfTheParentCrn()
    {
        var apc = Course(1, "APC", "100", 11);
        var apcOtherUnits = apc with { Id = 9 };
        var npb = Course(2, "NPB", "123", 29);

        var link = Assert.Single(BuildLinks([apc, apcOtherUnits, npb], Banner("A0", apc, npb), [], NoParents));

        Assert.Equal(npb, link.Child);
    }

    #endregion

    #region ApplyAsync

    [Fact]
    public async Task ApplyAsync_LinksCourses_AndLeavesNoMergeableEffortOnTheChild()
    {
        var ct = TestContext.Current.CancellationToken;
        var apc = AddCourse(1, "APC", "100");
        var npb = AddCourse(2, "NPB", "123");
        AddBanner(apc, "A0");
        AddBanner(npb, "A0");
        AddPerson(10);
        AddPerson(11);
        AddPerson(12);
        // Same on both courses: the child copy is dropped
        AddRecord(100, apc, 10, "VAR", hours: 0);
        AddRecord(101, npb, 10, "VAR", hours: 0);
        // Only on the child: moved to the parent
        AddRecord(102, npb, 11, "LEC", hours: 8);
        // Different hours on each: left for staff
        AddRecord(103, apc, 12, "VAR", hours: 20);
        AddRecord(104, npb, 12, "VAR", hours: 60);
        await _context.SaveChangesAsync(ct);
        await _coursesContext.SaveChangesAsync(ct);

        var result = await _service.ApplyAsync(TestTermCode, ct: ct);

        var relationship = Assert.Single(_context.CourseRelationships);
        Assert.Equal((apc.Id, npb.Id, CrossListType),
            (relationship.ParentCourseId, relationship.ChildCourseId, relationship.RelationshipType));
        Assert.DoesNotContain(_context.Records, r => r.Id == 101);
        var moved = _context.Records.Single(r => r.Id == 102);
        Assert.Equal((apc.Id, apc.Crn), (moved.CourseId, moved.Crn));
        Assert.Equal(npb.Id, _context.Records.Single(r => r.Id == 104).CourseId);
        Assert.Contains("VAR effort on NPB 123-001 differs from APC 100-001", Assert.Single(result.Conflicts));
    }

    [Fact]
    public async Task ApplyAsync_IsIdempotent_AndHonorsOnlyCrn()
    {
        var ct = TestContext.Current.CancellationToken;
        var apc = AddCourse(1, "APC", "100");
        var npb = AddCourse(2, "NPB", "123");
        var epi = AddCourse(3, "EPI", "206");
        var mpm = AddCourse(4, "MPM", "206");
        AddBanner(apc, "A0");
        AddBanner(npb, "A0");
        AddBanner(epi, "U6");
        AddBanner(mpm, "U6");
        await _context.SaveChangesAsync(ct);
        await _coursesContext.SaveChangesAsync(ct);

        var first = await _service.ApplyAsync(TestTermCode, npb.Crn, ct);
        var second = await _service.ApplyAsync(TestTermCode, npb.Crn, ct);

        Assert.Equal(npb.Id, Assert.Single(first.Links).Child.Id);
        Assert.Empty(second.Links);
        Assert.Single(_context.CourseRelationships);
    }

    private EffortCourse AddCourse(int id, string subj, string crse)
    {
        var course = new EffortCourse
        {
            Id = id,
            Crn = $"1000{id}",
            TermCode = TestTermCode,
            SubjCode = subj,
            CrseNumb = crse,
            SeqNumb = "001",
            Enrollment = 10,
            Units = 4,
            CustDept = "APC"
        };
        _context.Courses.Add(course);
        return course;
    }

    private void AddBanner(EffortCourse course, string group) =>
        _coursesContext.Baseinfos.Add(new Baseinfo
        {
            BaseinfoPkey = course.Crn,
            BaseinfoTermCode = TestTermCode.ToString(),
            BaseinfoSubjCode = course.SubjCode,
            BaseinfoCrseNumb = course.CrseNumb,
            BaseinfoSeqNumb = course.SeqNumb,
            BaseinfoCrn = course.Crn,
            BaseinfoTitle = "Course",
            BaseinfoUnitType = "F",
            BaseinfoUnitLow = 4,
            BaseinfoUnitHigh = 4,
            BaseinfoCollCode = "VM",
            BaseinfoDeptCode = course.SubjCode,
            BaseinfoXlistFlag = "Y",
            BaseinfoXlistGroup = group
        });

    private void AddPerson(int personId) =>
        _context.Persons.Add(new EffortPerson
        {
            PersonId = personId,
            TermCode = TestTermCode,
            FirstName = "FIRST",
            LastName = $"LAST{personId}"
        });

    private void AddRecord(int id, EffortCourse course, int personId, string effortType, int hours) =>
        _context.Records.Add(new EffortRecord
        {
            Id = id,
            CourseId = course.Id,
            PersonId = personId,
            TermCode = TestTermCode,
            EffortTypeId = effortType,
            RoleId = 1,
            Hours = hours,
            Crn = course.Crn
        });

    #endregion
}
