using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Data;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for managing evaluation data from the evalHarvest database.
/// </summary>
public class EvalHarvestService : IEvalHarvestService
{
    private readonly EvalHarvestDbContext _evalContext;
    private readonly EffortDbContext _effortContext;
    private readonly VIPERContext _viperContext;
    private readonly ILogger<EvalHarvestService> _logger;

    public EvalHarvestService(
        EvalHarvestDbContext evalContext,
        EffortDbContext effortContext,
        VIPERContext viperContext,
        ILogger<EvalHarvestService> logger)
    {
        _evalContext = evalContext;
        _effortContext = effortContext;
        _viperContext = viperContext;
        _logger = logger;
    }

    public async Task<CourseEvaluationStatusDto> GetCourseEvaluationStatusAsync(
        int courseId, CancellationToken ct = default)
    {
        // Get the course from effort DB
        var course = await _effortContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new InvalidOperationException($"Course {courseId} not found");

        // Collect this course + child courses
        var childRelationships = await _effortContext.CourseRelationships
            .AsNoTracking()
            .Where(r => r.ParentCourseId == courseId)
            .ToListAsync(ct);

        var childCourseIds = childRelationships.Select(r => r.ChildCourseId).ToList();
        var allCourseIds = new List<int> { courseId };
        allCourseIds.AddRange(childCourseIds);

        var allCourses = await _effortContext.Courses
            .AsNoTracking()
            .Where(c => allCourseIds.Contains(c.Id))
            .ToListAsync(ct);

        // Build course info DTOs
        var courseInfos = allCourses.Select(c => new EvalCourseInfoDto
        {
            CourseId = c.Id,
            CourseName = $"{c.SubjCode.Trim()} {c.CrseNumb.Trim()}",
            Crn = c.Crn.Trim()
        }).ToList();

        // Get all effort records for these courses to find instructors
        var effortRecords = await _effortContext.Records
            .AsNoTracking()
            .Where(r => allCourseIds.Contains(r.CourseId))
            .ToListAsync(ct);

        // Get unique person IDs from effort records
        var personIds = effortRecords.Select(r => r.PersonId).Distinct().ToList();

        // Get VIPER persons (for MothraId and MailId lookup)
        var viperPersons = await _viperContext.People
            .AsNoTracking()
            .Where(p => personIds.Contains(p.PersonId))
            .Select(p => new { p.PersonId, p.MothraId, p.MailId, p.FirstName, p.LastName })
            .ToListAsync(ct);

        // Get effort persons for display names
        var effortPersons = await _effortContext.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == course.TermCode && personIds.Contains(p.PersonId))
            .ToListAsync(ct);

        // Query evalHarvest for all CRNs at once
        var crns = allCourses.Select(c => int.TryParse(c.Crn.Trim(), out var crn) ? crn : 0)
            .Where(c => c > 0)
            .ToList();
        var termCode = course.TermCode;

        // Check which CRNs have course records in evalHarvest (and their adhoc status)
        var ehCourses = await _evalContext.Courses
            .AsNoTracking()
            .Where(c => crns.Contains(c.Crn) && c.TermCode == termCode && c.FacilitatorEvalId == 0)
            .ToListAsync(ct);

        // Get overall eval data for all CRNs
        var evalData = await _evalContext.Quants
            .AsNoTracking()
            .Include(q => q.Question)
            .Where(q => crns.Contains(q.Question.Crn)
                && q.Question.TermCode == termCode
                && q.Question.IsOverall
                && q.MailId != null)
            .ToListAsync(ct);

        // Get eh_People for mailId -> mothraId mapping
        var evalMailIds = evalData.Select(q => q.MailId!).Distinct().ToList();
        var ehPeople = await _evalContext.People
            .AsNoTracking()
            .Where(p => evalMailIds.Contains(p.MailId) && p.TermCode == termCode)
            .ToListAsync(ct);

        // Build mothraId -> mailId lookup from evalHarvest people
        var mothraToMailId = ehPeople
            .GroupBy(p => p.MothraId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().MailId,
                StringComparer.OrdinalIgnoreCase);

        // Determine if any course has CERE data (course_adhoc = false/null)
        var hasCereData = ehCourses.Any(c => c.IsAdHoc != true);

        // Build instructor eval status
        var instructors = new List<InstructorEvalStatusDto>();
        foreach (var personId in personIds)
        {
            var viperPerson = viperPersons.FirstOrDefault(p => p.PersonId == personId);
            var effortPerson = effortPersons.FirstOrDefault(p => p.PersonId == personId);
            if (viperPerson == null) continue;

            var mothraId = viperPerson.MothraId;
            var displayName = effortPerson != null
                ? $"{effortPerson.LastName}, {effortPerson.FirstName}"
                : $"{viperPerson.LastName}, {viperPerson.FirstName}";

            // Find this instructor's mailId in evalHarvest
            mothraToMailId.TryGetValue(mothraId, out var instructorMailId);
            // Also check via VIPER person's MailId directly
            if (instructorMailId == null && viperPerson.MailId != null)
            {
                instructorMailId = viperPerson.MailId;
            }

            var evaluations = new List<CourseEvalEntryDto>();
            foreach (var effortCourse in allCourses)
            {
                var crnStr = effortCourse.Crn.Trim();
                if (!int.TryParse(crnStr, out var crnInt)) continue;

                var ehCourse = ehCourses.FirstOrDefault(c => c.Crn == crnInt);

                // Find eval data for this instructor + CRN
                var instructorEval = evalData.FirstOrDefault(q =>
                    q.Question.Crn == crnInt
                    && q.MailId != null
                    && string.Equals(q.MailId, instructorMailId, StringComparison.OrdinalIgnoreCase));

                string status;
                bool canEdit;
                if (instructorEval != null)
                {
                    status = ehCourse?.IsAdHoc == true ? "AdHoc" : "CERE";
                    canEdit = ehCourse?.IsAdHoc == true;
                }
                else
                {
                    status = "None";
                    canEdit = !hasCereData;
                }

                evaluations.Add(new CourseEvalEntryDto
                {
                    CourseId = effortCourse.Id,
                    Crn = crnStr,
                    Status = status,
                    CanEdit = canEdit,
                    QuantId = instructorEval?.QuantId,
                    Mean = instructorEval?.Mean,
                    StandardDeviation = instructorEval?.Sd,
                    Respondents = instructorEval?.Respondents,
                    Count1 = instructorEval?.Count1N,
                    Count2 = instructorEval?.Count2N,
                    Count3 = instructorEval?.Count3N,
                    Count4 = instructorEval?.Count4N,
                    Count5 = instructorEval?.Count5N
                });
            }

            instructors.Add(new InstructorEvalStatusDto
            {
                PersonId = personId,
                MothraId = mothraId,
                InstructorName = displayName,
                Evaluations = evaluations
            });
        }

        return new CourseEvaluationStatusDto
        {
            CanEditAdHoc = !hasCereData,
            Instructors = instructors,
            Courses = courseInfos
        };
    }

    public async Task<AdHocEvalResultDto> CreateAdHocEvaluationAsync(
        CreateAdHocEvalRequest request, CancellationToken ct = default)
    {
        // Get the effort course
        var course = await _effortContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);

        if (course == null)
        {
            return new AdHocEvalResultDto { Success = false, Error = "Course not found" };
        }

        if (!int.TryParse(course.Crn.Trim(), out var crn))
        {
            return new AdHocEvalResultDto { Success = false, Error = "Invalid CRN" };
        }

        // Check CERE blocking — query without AsNoTracking because the entity is
        // used for upsert below (setting IsAdHoc = true on an existing record)
        var ehCourse = await _evalContext.Courses
            .FirstOrDefaultAsync(c => c.Crn == crn
                && c.TermCode == course.TermCode
                && c.FacilitatorEvalId == 0, ct);

        if (IsCereBlocked(ehCourse))
        {
            return new AdHocEvalResultDto
            {
                Success = false,
                Error = "Cannot create ad-hoc evaluation: CERE data exists for this course"
            };
        }

        // Look up the instructor's VIPER person record
        var viperPerson = await _viperContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MothraId == request.MothraId, ct);

        if (viperPerson == null)
        {
            _logger.LogWarning("Person not found for MothraId {MothraId}",
                LogSanitizer.SanitizeId(request.MothraId));
            return new AdHocEvalResultDto { Success = false, Error = "Instructor not found" };
        }

        var mailId = viperPerson.MailId;
        if (string.IsNullOrEmpty(mailId))
        {
            return new AdHocEvalResultDto { Success = false, Error = "Instructor has no email address" };
        }

        // Get effort person for name/dept info
        var effortPerson = await _effortContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == viperPerson.PersonId
                && p.TermCode == course.TermCode, ct);

        // Wrap lookup + insert in a serializable transaction to prevent duplicate ad-hoc evals
        // from concurrent requests for the same (term, CRN, mailId)
        await using var transaction = await _evalContext.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        // Duplicate check inside the transaction so concurrent requests serialize
        var existingQuant = await _evalContext.Quants
            .Include(q => q.Question)
            .FirstOrDefaultAsync(q => q.Question.Crn == crn
                && q.Question.TermCode == course.TermCode
                && q.Question.IsOverall
                && q.MailId != null
                && q.MailId == mailId, ct);

        if (existingQuant != null)
        {
            await transaction.RollbackAsync(ct);
            return new AdHocEvalResultDto
            {
                Success = false,
                Error = "An evaluation already exists for this instructor on this course"
            };
        }

        // Upsert eh_Courses record with adhoc=true
        if (ehCourse == null)
        {
            ehCourse = new EhCourse
            {
                Crn = crn,
                TermCode = course.TermCode,
                FacilitatorEvalId = 0,
                SubjCode = course.SubjCode.Trim(),
                CrseNumb = course.CrseNumb.Trim(),
                Enrollment = course.Enrollment,
                HomeDept = course.CustDept.Trim(),
                IsAdHoc = true,
                Sequence = course.SeqNumb.Trim()
            };
            _evalContext.Courses.Add(ehCourse);
        }
        else
        {
            ehCourse.IsAdHoc = true;
        }

        // Upsert eh_People record
        var ehPerson = await _evalContext.People
            .FirstOrDefaultAsync(p => p.MailId == mailId
                && p.TermCode == course.TermCode, ct);

        if (ehPerson == null)
        {
            ehPerson = new EhPerson
            {
                MailId = mailId,
                TermCode = course.TermCode,
                MothraId = request.MothraId,
                LoginId = viperPerson.LoginId ?? "",
                FirstName = effortPerson?.FirstName ?? viperPerson.FirstName,
                LastName = effortPerson?.LastName ?? viperPerson.LastName,
                TeachingDept = effortPerson?.EffortDept?.Trim() ?? course.CustDept.Trim()
            };
            _evalContext.People.Add(ehPerson);
        }

        // Create eh_Questions record for overall teaching effectiveness
        var question = new EhQuestion
        {
            Text = "Overall teaching effectiveness",
            Type = "5-pt. Scale (Low/High)",
            Order = 1,
            Crn = crn,
            TermCode = course.TermCode,
            IsOverall = true,
            EvaluateeType = "I",
            FacilitatorEvalId = 0
        };
        _evalContext.Questions.Add(question);

        await _evalContext.SaveChangesAsync(ct);

        // Create eh_Quant record (needs question.QuestId from first save)
        var quant = new EhQuant
        {
            QuestionIdFk = question.QuestId,
            MailId = mailId,
            NoOpN = 0,
            NoOpP = 0,
            Mean = request.Mean,
            Sd = request.StandardDeviation,
            Enrolled = course.Enrollment,
            Respondents = request.Respondents,
            EvaluateeType = "I"
        };
        ApplyRatingCounts(quant, request.Count1, request.Count2, request.Count3, request.Count4, request.Count5);
        _evalContext.Quants.Add(quant);

        await _evalContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Ad-hoc evaluation created: QuantId={QuantId} for CRN={Crn}, MothraId={MothraId}",
            quant.QuantId, LogSanitizer.SanitizeId(crn.ToString()), LogSanitizer.SanitizeId(request.MothraId));

        return new AdHocEvalResultDto { Success = true, QuantId = quant.QuantId };
    }

    public async Task<AdHocEvalResultDto> UpdateAdHocEvaluationAsync(
        int courseId, int quantId, UpdateAdHocEvalRequest request, CancellationToken ct = default)
    {
        var quant = await _evalContext.Quants
            .Include(q => q.Question)
            .FirstOrDefaultAsync(q => q.QuantId == quantId, ct);

        if (quant == null)
        {
            return new AdHocEvalResultDto { Success = false, Error = "Evaluation record not found" };
        }

        // Verify the quant belongs to the authorized course (IDOR protection)
        if (!await QuantBelongsToCourseAsync(quant, courseId, ct))
        {
            return new AdHocEvalResultDto { Success = false, Error = "Evaluation does not belong to this course" };
        }

        // Fail-closed: only allow update if the course record is explicitly ad-hoc
        var ehCourse = await GetEhCourseAsync(quant.Question.Crn, quant.Question.TermCode, ct);

        if (ehCourse?.IsAdHoc != true)
        {
            return new AdHocEvalResultDto
            {
                Success = false,
                Error = "Cannot update: this is not an ad-hoc evaluation"
            };
        }

        // Update quant values
        ApplyRatingCounts(quant, request.Count1, request.Count2, request.Count3, request.Count4, request.Count5);
        quant.Mean = request.Mean;
        quant.Sd = request.StandardDeviation;
        quant.Respondents = request.Respondents;

        await _evalContext.SaveChangesAsync(ct);

        _logger.LogInformation("Ad-hoc evaluation updated: QuantId={QuantId}", quantId);

        return new AdHocEvalResultDto { Success = true, QuantId = quantId };
    }

    public async Task<bool> DeleteAdHocEvaluationAsync(int courseId, int quantId, CancellationToken ct = default)
    {
        var quant = await _evalContext.Quants
            .Include(q => q.Question)
            .FirstOrDefaultAsync(q => q.QuantId == quantId, ct);

        if (quant == null)
        {
            return false;
        }

        // Verify the quant belongs to the authorized course (IDOR protection)
        if (!await QuantBelongsToCourseAsync(quant, courseId, ct))
        {
            return false;
        }

        // Fail-closed: only allow delete if the course record is explicitly ad-hoc
        var ehCourse = await GetEhCourseAsync(quant.Question.Crn, quant.Question.TermCode, ct);

        if (ehCourse?.IsAdHoc != true)
        {
            return false;
        }

        _evalContext.Quants.Remove(quant);
        await _evalContext.SaveChangesAsync(ct);

        _logger.LogInformation("Ad-hoc evaluation deleted: QuantId={QuantId}", quantId);

        return true;
    }

    /// <summary>
    /// Verifies a quant record belongs to the given effort course by matching CRN + termCode.
    /// </summary>
    private async Task<bool> QuantBelongsToCourseAsync(EhQuant quant, int courseId, CancellationToken ct)
    {
        var course = await _effortContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null) return false;

        if (!int.TryParse(course.Crn.Trim(), out var courseCrn)) return false;

        return quant.Question.Crn == courseCrn && quant.Question.TermCode == course.TermCode;
    }

    private async Task<EhCourse?> GetEhCourseAsync(int crn, int termCode, CancellationToken ct)
    {
        return await _evalContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Crn == crn
                && c.TermCode == termCode
                && c.FacilitatorEvalId == 0, ct);
    }

    private static bool IsCereBlocked(EhCourse? ehCourse)
    {
        return ehCourse != null && ehCourse.IsAdHoc != true;
    }

    /// <summary>
    /// Sets rating count fields (raw counts and percentages) on a quant record.
    /// </summary>
    private static void ApplyRatingCounts(EhQuant quant, int count1, int count2, int count3, int count4, int count5)
    {
        var total = count1 + count2 + count3 + count4 + count5;
        quant.Count1N = count1;
        quant.Count2N = count2;
        quant.Count3N = count3;
        quant.Count4N = count4;
        quant.Count5N = count5;
        quant.Count1P = total > 0 ? (double)count1 / total * 100 : 0;
        quant.Count2P = total > 0 ? (double)count2 / total * 100 : 0;
        quant.Count3P = total > 0 ? (double)count3 / total * 100 : 0;
        quant.Count4P = total > 0 ? (double)count4 / total * 100 : 0;
        quant.Count5P = total > 0 ? (double)count5 / total * 100 : 0;
    }
}
