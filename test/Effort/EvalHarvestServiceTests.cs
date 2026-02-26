using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Data;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for EvalHarvestService pure/static helper methods.
/// </summary>
public sealed class EvalHarvestServiceTests
{
    #region ApplyRatingCounts

    [Fact]
    public void ApplyRatingCounts_SetsCountsAndPercentages()
    {
        var quant = new EhQuant();
        EvalHarvestService.ApplyRatingCounts(quant, 1, 2, 3, 4, 10);

        Assert.Equal(1, quant.Count1N);
        Assert.Equal(2, quant.Count2N);
        Assert.Equal(3, quant.Count3N);
        Assert.Equal(4, quant.Count4N);
        Assert.Equal(10, quant.Count5N);

        Assert.Equal(20, quant.Respondents);
        Assert.Equal(5.0, quant.Count1P);
        Assert.Equal(10.0, quant.Count2P);
        Assert.Equal(15.0, quant.Count3P);
        Assert.Equal(20.0, quant.Count4P);
        Assert.Equal(50.0, quant.Count5P);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesMeanCorrectly()
    {
        var quant = new EhQuant();
        // All respondents gave a 3 → mean should be exactly 3.0
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 10, 0, 0);

        Assert.Equal(3.0, quant.Mean);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesMeanWithMixedCounts()
    {
        var quant = new EhQuant();
        // 2×1 + 3×5 = 17, total=5, mean=3.4
        EvalHarvestService.ApplyRatingCounts(quant, 2, 0, 0, 0, 3);

        Assert.Equal(3.4, quant.Mean);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesStandardDeviation()
    {
        var quant = new EhQuant();
        // All same rating → SD should be 0
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 0, 5, 0);

        Assert.Equal(0.0, quant.Sd);
    }

    [Fact]
    public void ApplyRatingCounts_ComputesStandardDeviationWithSpread()
    {
        var quant = new EhQuant();
        // 5×1, 5×5 → mean=3.0, variance=((1-3)²×5 + (5-3)²×5)/10 = (20+20)/10 = 4, SD=2.0
        EvalHarvestService.ApplyRatingCounts(quant, 5, 0, 0, 0, 5);

        Assert.Equal(3.0, quant.Mean);
        Assert.Equal(2.0, quant.Sd);
    }

    [Fact]
    public void ApplyRatingCounts_AllZeros_SetsMeanAndSdToZero()
    {
        var quant = new EhQuant();
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 0, 0, 0);

        Assert.Equal(0, quant.Respondents);
        Assert.Equal(0.0, quant.Mean);
        Assert.Equal(0.0, quant.Sd);
        Assert.Equal(0.0, quant.Count1P);
    }

    [Fact]
    public void ApplyRatingCounts_SingleRespondent()
    {
        var quant = new EhQuant();
        EvalHarvestService.ApplyRatingCounts(quant, 0, 0, 0, 0, 1);

        Assert.Equal(1, quant.Respondents);
        Assert.Equal(5.0, quant.Mean);
        Assert.Equal(0.0, quant.Sd);
        Assert.Equal(100.0, quant.Count5P);
    }

    #endregion

    #region HasAnyResponses

    [Fact]
    public void HasAnyResponses_AllZeros_ReturnsFalse()
    {
        var request = new UpdateAdHocEvalRequest
        {
            Count1 = 0,
            Count2 = 0,
            Count3 = 0,
            Count4 = 0,
            Count5 = 0
        };
        Assert.False(EvalHarvestService.HasAnyResponses(request));
    }

    [Fact]
    public void HasAnyResponses_OneNonZero_ReturnsTrue()
    {
        var request = new UpdateAdHocEvalRequest
        {
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };
        Assert.True(EvalHarvestService.HasAnyResponses(request));
    }

    #endregion

    #region IsHarvestedEvalBlocked

    [Fact]
    public void IsHarvestedEvalBlocked_NullCourse_ReturnsFalse()
    {
        Assert.False(EvalHarvestService.IsHarvestedEvalBlocked(null));
    }

    [Fact]
    public void IsHarvestedEvalBlocked_AdHocTrue_ReturnsFalse()
    {
        var course = new EhCourse { IsAdHoc = true };
        Assert.False(EvalHarvestService.IsHarvestedEvalBlocked(course));
    }

    [Fact]
    public void IsHarvestedEvalBlocked_AdHocFalse_ReturnsTrue()
    {
        var course = new EhCourse { IsAdHoc = false };
        Assert.True(EvalHarvestService.IsHarvestedEvalBlocked(course));
    }

    [Fact]
    public void IsHarvestedEvalBlocked_AdHocNull_ReturnsTrue()
    {
        var course = new EhCourse { IsAdHoc = null };
        Assert.True(EvalHarvestService.IsHarvestedEvalBlocked(course));
    }

    #endregion
}

/// <summary>
/// Integration tests for EvalHarvestService async methods using in-memory databases.
/// Tests CRUD operations, IDOR protection, and harvested eval blocking logic.
/// </summary>
public sealed class EvalHarvestServiceIntegrationTests : IDisposable
{
    private const int TestTermCode = 202410;
    private const int TestCourseId = 1;
    private const int TestChildCourseId = 2;
    private const int TestPersonId = 999;
    private const string TestMothraId = "testuser";
    private const string TestMailId = "testuser@ucdavis.edu";
    private const int TestCrn = 12345;
    private const int TestChildCrn = 12346;

    private readonly EffortDbContext _effortContext;
    private readonly EvalHarvestDbContext _evalContext;
    private readonly VIPERContext _viperContext;
    private readonly EvalHarvestService _service;

    public EvalHarvestServiceIntegrationTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _effortContext = new EffortDbContext(effortOptions);

        var evalOptions = new DbContextOptionsBuilder<EvalHarvestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _evalContext = new EvalHarvestDbContext(evalOptions);

        var viperOptions = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _viperContext = new VIPERContext(viperOptions);

        var loggerMock = new Mock<ILogger<EvalHarvestService>>();
        _service = new EvalHarvestService(_evalContext, _effortContext, _viperContext, loggerMock.Object);

        SeedBaseDataAsync().GetAwaiter().GetResult();
    }

    private async Task SeedBaseDataAsync()
    {
        // Effort term
        _effortContext.Terms.Add(new EffortTerm { TermCode = TestTermCode, OpenedDate = DateTime.Now });

        // Effort courses
        _effortContext.Courses.AddRange(
            new EffortCourse { Id = TestCourseId, TermCode = TestTermCode, Crn = TestCrn.ToString(), SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = TestChildCourseId, TermCode = TestTermCode, Crn = TestChildCrn.ToString(), SubjCode = "DVM", CrseNumb = "443", SeqNumb = "002", Enrollment = 10, Units = 4, CustDept = "DVM" }
        );

        // Effort persons
        _effortContext.Persons.Add(new EffortPerson { PersonId = TestPersonId, TermCode = TestTermCode, FirstName = "Test", LastName = "User", EffortDept = "DVM" });

        // Effort records (link instructor to course)
        _effortContext.Records.Add(new EffortRecord { Id = 1, CourseId = TestCourseId, PersonId = TestPersonId, TermCode = TestTermCode, EffortTypeId = "LEC", RoleId = 1, Crn = TestCrn.ToString() });

        await _effortContext.SaveChangesAsync();

        // VIPER person
        _viperContext.People.Add(new Person
        {
            PersonId = TestPersonId,
            MothraId = TestMothraId,
            MailId = TestMailId,
            LoginId = "testuser",
            FirstName = "Test",
            LastName = "User",
            FullName = "Test User",
            ClientId = "C999",
            Current = 1
        });
        await _viperContext.SaveChangesAsync();
    }

    #region GetCourseEvaluationStatusAsync

    [Fact]
    public async Task GetCourseEvaluationStatus_NoEvalData_ReturnsNoneStatus()
    {
        var result = await _service.GetCourseEvaluationStatusAsync(TestCourseId);

        Assert.True(result.CanEditAdHoc);
        Assert.Single(result.Instructors);
        var instructor = result.Instructors[0];
        Assert.Equal(TestPersonId, instructor.PersonId);
        Assert.Equal(TestMothraId, instructor.MothraId);
        Assert.Single(instructor.Evaluations);
        Assert.Equal("None", instructor.Evaluations[0].Status);
        Assert.True(instructor.Evaluations[0].CanEdit);
    }

    [Fact]
    public async Task GetCourseEvaluationStatus_WithHarvestedEvalData_ReturnsHarvestedEvalStatus()
    {
        // Seed harvested eval data
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = false });
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId, Mean = 4.5, Respondents = 10 });
        await _evalContext.SaveChangesAsync();

        var result = await _service.GetCourseEvaluationStatusAsync(TestCourseId);

        Assert.False(result.CanEditAdHoc);
        var eval = result.Instructors[0].Evaluations[0];
        Assert.Equal("HarvestedEval", eval.Status);
        Assert.False(eval.CanEdit);
        Assert.Equal(4.5, eval.Mean);
        Assert.Equal(10, eval.Respondents);
    }

    [Fact]
    public async Task GetCourseEvaluationStatus_WithAdHocData_ReturnsAdHocStatus()
    {
        // Seed ad-hoc eval data
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = true });
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId, Mean = 3.0, Respondents = 5 });
        await _evalContext.SaveChangesAsync();

        var result = await _service.GetCourseEvaluationStatusAsync(TestCourseId);

        Assert.True(result.CanEditAdHoc);
        var eval = result.Instructors[0].Evaluations[0];
        Assert.Equal("AdHoc", eval.Status);
        Assert.True(eval.CanEdit);
    }

    [Fact]
    public async Task GetCourseEvaluationStatus_WithChildCourses_IncludesChildInstructors()
    {
        // Add course relationship
        _effortContext.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = TestCourseId, ChildCourseId = TestChildCourseId, RelationshipType = "CrossList" });

        // Add a second instructor on the child course
        _effortContext.Persons.Add(new EffortPerson { PersonId = 1001, TermCode = TestTermCode, FirstName = "Child", LastName = "Instructor", EffortDept = "DVM" });
        _effortContext.Records.Add(new EffortRecord { Id = 2, CourseId = TestChildCourseId, PersonId = 1001, TermCode = TestTermCode, EffortTypeId = "LEC", RoleId = 1, Crn = TestChildCrn.ToString() });
        await _effortContext.SaveChangesAsync();

        _viperContext.People.Add(new Person
        {
            PersonId = 1001,
            MothraId = "child01",
            MailId = "child@ucdavis.edu",
            LoginId = "child01",
            FirstName = "Child",
            LastName = "Instructor",
            FullName = "Child Instructor",
            ClientId = "C1001",
            Current = 1
        });
        await _viperContext.SaveChangesAsync();

        var result = await _service.GetCourseEvaluationStatusAsync(TestCourseId);

        Assert.Equal(2, result.Courses.Count);
        Assert.Equal(2, result.Instructors.Count);
        // Each instructor should have evaluations for both courses
        Assert.Equal(2, result.Instructors[0].Evaluations.Count);
        Assert.Equal(2, result.Instructors[1].Evaluations.Count);
    }

    [Fact]
    public async Task GetCourseEvaluationStatus_CourseNotFound_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetCourseEvaluationStatusAsync(9999));
    }

    [Fact]
    public async Task GetCourseEvaluationStatus_HarvestedEvalBlocksAdHocGlobally()
    {
        // Harvested eval data on the course — even with no eval data for the specific instructor,
        // canEditAdHoc should be false and "None" entries should not be editable
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = false });
        await _evalContext.SaveChangesAsync();

        var result = await _service.GetCourseEvaluationStatusAsync(TestCourseId);

        Assert.False(result.CanEditAdHoc);
        var eval = result.Instructors[0].Evaluations[0];
        Assert.Equal("None", eval.Status);
        Assert.False(eval.CanEdit);
    }

    #endregion

    #region CreateAdHocEvaluationAsync

    [Fact]
    public async Task CreateAdHocEvaluation_HappyPath_CreatesAllRecords()
    {
        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = TestMothraId,
            Count1 = 1,
            Count2 = 2,
            Count3 = 3,
            Count4 = 4,
            Count5 = 10
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.QuantId);

        // Verify records created in evalHarvest DB
        var ehCourse = await _evalContext.Courses.FirstOrDefaultAsync(c => c.Crn == TestCrn && c.TermCode == TestTermCode);
        Assert.NotNull(ehCourse);
        Assert.True(ehCourse.IsAdHoc);

        var ehPerson = await _evalContext.People.FirstOrDefaultAsync(p => p.MailId == TestMailId);
        Assert.NotNull(ehPerson);
        Assert.Equal(TestMothraId, ehPerson.MothraId);

        var question = await _evalContext.Questions.FirstOrDefaultAsync(q => q.Crn == TestCrn && q.TermCode == TestTermCode);
        Assert.NotNull(question);
        Assert.True(question.IsOverall);

        var quant = await _evalContext.Quants.FirstOrDefaultAsync(q => q.QuantId == result.QuantId);
        Assert.NotNull(quant);
        Assert.Equal(20, quant.Respondents);
        Assert.Equal(TestMailId, quant.MailId);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_ZeroCounts_ReturnsError()
    {
        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = TestMothraId,
            Count1 = 0,
            Count2 = 0,
            Count3 = 0,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("rating count", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_CourseNotFound_ReturnsError()
    {
        var request = new CreateAdHocEvalRequest
        {
            CourseId = 9999,
            MothraId = TestMothraId,
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Course not found", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_InvalidCrn_ReturnsError()
    {
        // Seed an effort course whose CRN is non-numeric (data quality edge case)
        _effortContext.Courses.Add(new EffortCourse { Id = 100, TermCode = TestTermCode, Crn = "INVALID", SubjCode = "DVM", CrseNumb = "500", SeqNumb = "001", Enrollment = 10, Units = 4, CustDept = "DVM" });
        await _effortContext.SaveChangesAsync();

        var request = new CreateAdHocEvalRequest
        {
            CourseId = 100,
            MothraId = TestMothraId,
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Invalid CRN", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_HarvestedEvalBlocked_ReturnsError()
    {
        // Seed non-adhoc course record (harvested eval data)
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = false });
        await _evalContext.SaveChangesAsync();

        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = TestMothraId,
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("harvested eval data exists", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_InstructorNotFound_ReturnsError()
    {
        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = "nonexistent",
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Instructor not found", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_InstructorNotOnCourse_ReturnsError()
    {
        // Add a VIPER person who is NOT linked to the test course via effort records
        _viperContext.People.Add(new Person
        {
            PersonId = 2000,
            MothraId = "other01",
            MailId = "other@ucdavis.edu",
            LoginId = "other01",
            FirstName = "Other",
            LastName = "Person",
            FullName = "Other Person",
            ClientId = "C2000",
            Current = 1
        });
        await _viperContext.SaveChangesAsync();

        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = "other01",
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("not associated with this course", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_InstructorNoEmail_ReturnsError()
    {
        // Add a VIPER person with no email, and link to course
        _viperContext.People.Add(new Person
        {
            PersonId = 3000,
            MothraId = "noemail",
            MailId = null,
            LoginId = "noemail",
            FirstName = "No",
            LastName = "Email",
            FullName = "No Email",
            ClientId = "C3000",
            Current = 1
        });
        await _viperContext.SaveChangesAsync();

        _effortContext.Records.Add(new EffortRecord { Id = 10, CourseId = TestCourseId, PersonId = 3000, TermCode = TestTermCode, EffortTypeId = "LEC", RoleId = 1, Crn = TestCrn.ToString() });
        await _effortContext.SaveChangesAsync();

        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = "noemail",
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("no email", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_DuplicateEval_ReturnsError()
    {
        // Seed an existing eval for this instructor + CRN
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId, Mean = 4.0, Respondents = 5 });
        await _evalContext.SaveChangesAsync();

        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = TestMothraId,
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task CreateAdHocEvaluation_UpsertsExistingAdHocCourse()
    {
        // Seed an existing adhoc course record
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = true });
        await _evalContext.SaveChangesAsync();

        var request = new CreateAdHocEvalRequest
        {
            CourseId = TestCourseId,
            MothraId = TestMothraId,
            Count1 = 0,
            Count2 = 0,
            Count3 = 1,
            Count4 = 0,
            Count5 = 0
        };

        var result = await _service.CreateAdHocEvaluationAsync(request);

        Assert.True(result.Success);
        // Should not create a duplicate course record
        var courseCount = await _evalContext.Courses.CountAsync(c => c.Crn == TestCrn && c.TermCode == TestTermCode);
        Assert.Equal(1, courseCount);
    }

    #endregion

    #region UpdateAdHocEvaluationAsync

    [Fact]
    public async Task UpdateAdHocEvaluation_HappyPath_UpdatesCounts()
    {
        // Seed ad-hoc eval data
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = true });
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        var quant = new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId, Mean = 3.0, Respondents = 5 };
        _evalContext.Quants.Add(quant);
        await _evalContext.SaveChangesAsync();

        var request = new UpdateAdHocEvalRequest { Count1 = 2, Count2 = 3, Count3 = 5, Count4 = 8, Count5 = 2 };

        var result = await _service.UpdateAdHocEvaluationAsync(TestCourseId, 1, request);

        Assert.True(result.Success);
        var updated = await _evalContext.Quants.FirstAsync(q => q.QuantId == 1);
        Assert.Equal(20, updated.Respondents);
        Assert.Equal(2, updated.Count1N);
        Assert.Equal(8, updated.Count4N);
    }

    [Fact]
    public async Task UpdateAdHocEvaluation_ZeroCounts_ReturnsError()
    {
        var request = new UpdateAdHocEvalRequest { Count1 = 0, Count2 = 0, Count3 = 0, Count4 = 0, Count5 = 0 };

        var result = await _service.UpdateAdHocEvaluationAsync(TestCourseId, 1, request);

        Assert.False(result.Success);
        Assert.Contains("rating count", result.Error);
    }

    [Fact]
    public async Task UpdateAdHocEvaluation_QuantNotFound_ReturnsError()
    {
        var request = new UpdateAdHocEvalRequest { Count1 = 0, Count2 = 0, Count3 = 1, Count4 = 0, Count5 = 0 };

        var result = await _service.UpdateAdHocEvaluationAsync(TestCourseId, 9999, request);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task UpdateAdHocEvaluation_IdorViolation_ReturnsError()
    {
        // Seed a quant on a DIFFERENT CRN than the authorized course
        var question = new EhQuestion { QuestId = 1, Crn = 99999, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId });
        await _evalContext.SaveChangesAsync();

        var request = new UpdateAdHocEvalRequest { Count1 = 0, Count2 = 0, Count3 = 1, Count4 = 0, Count5 = 0 };

        var result = await _service.UpdateAdHocEvaluationAsync(TestCourseId, 1, request);

        Assert.False(result.Success);
        Assert.Contains("does not belong", result.Error);
    }

    [Fact]
    public async Task UpdateAdHocEvaluation_NotAdHoc_ReturnsError()
    {
        // Seed harvested (non-adhoc) course + eval
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = false });
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId });
        await _evalContext.SaveChangesAsync();

        var request = new UpdateAdHocEvalRequest { Count1 = 0, Count2 = 0, Count3 = 1, Count4 = 0, Count5 = 0 };

        var result = await _service.UpdateAdHocEvaluationAsync(TestCourseId, 1, request);

        Assert.False(result.Success);
        Assert.Contains("not an ad-hoc evaluation", result.Error);
    }

    [Fact]
    public async Task UpdateAdHocEvaluation_NoEhCourseRecord_ReturnsError()
    {
        // Quant exists and belongs to the course, but there is no EhCourse record at all —
        // the service is fail-closed: no course record means not ad-hoc, so update is rejected.
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId });
        await _evalContext.SaveChangesAsync();

        var request = new UpdateAdHocEvalRequest { Count1 = 0, Count2 = 0, Count3 = 1, Count4 = 0, Count5 = 0 };

        var result = await _service.UpdateAdHocEvaluationAsync(TestCourseId, 1, request);

        Assert.False(result.Success);
        Assert.Contains("not an ad-hoc evaluation", result.Error);
    }

    #endregion

    #region DeleteAdHocEvaluationAsync

    [Fact]
    public async Task DeleteAdHocEvaluation_HappyPath_RemovesQuant()
    {
        // Seed ad-hoc eval
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = true });
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId });
        await _evalContext.SaveChangesAsync();

        var deleted = await _service.DeleteAdHocEvaluationAsync(TestCourseId, 1);

        Assert.True(deleted);
        Assert.Null(await _evalContext.Quants.FirstOrDefaultAsync(q => q.QuantId == 1));
    }

    [Fact]
    public async Task DeleteAdHocEvaluation_QuantNotFound_ReturnsFalse()
    {
        var deleted = await _service.DeleteAdHocEvaluationAsync(TestCourseId, 9999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAdHocEvaluation_IdorViolation_ReturnsFalse()
    {
        // Quant on a different CRN
        var question = new EhQuestion { QuestId = 1, Crn = 99999, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId });
        await _evalContext.SaveChangesAsync();

        var deleted = await _service.DeleteAdHocEvaluationAsync(TestCourseId, 1);

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAdHocEvaluation_NotAdHoc_ReturnsFalse()
    {
        // Harvested eval course
        _evalContext.Courses.Add(new EhCourse { Crn = TestCrn, TermCode = TestTermCode, FacilitatorEvalId = 0, SubjCode = "DVM", CrseNumb = "443", IsAdHoc = false });
        var question = new EhQuestion { QuestId = 1, Crn = TestCrn, TermCode = TestTermCode, IsOverall = true, FacilitatorEvalId = 0, Text = "Overall", Type = "5-pt", Order = 1 };
        _evalContext.Questions.Add(question);
        await _evalContext.SaveChangesAsync();
        _evalContext.Quants.Add(new EhQuant { QuantId = 1, QuestionIdFk = question.QuestId, MailId = TestMailId });
        await _evalContext.SaveChangesAsync();

        var deleted = await _service.DeleteAdHocEvaluationAsync(TestCourseId, 1);

        Assert.False(deleted);
    }

    #endregion

    public void Dispose()
    {
        _effortContext.Dispose();
        _evalContext.Dispose();
        _viperContext.Dispose();
    }
}
