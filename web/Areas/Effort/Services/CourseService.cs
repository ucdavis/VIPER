using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for course-related operations in the Effort system.
/// </summary>
public class CourseService : ICourseService
{
    private readonly EffortDbContext _context;
    private readonly CoursesContext _coursesContext;
    private readonly IEffortAuditService _auditService;
    private readonly ILogger<CourseService> _logger;

    /// <summary>
    /// Valid custodial department codes for the Effort system.
    /// </summary>
    private static readonly List<string> ValidCustDepts = new()
    {
        "APC", "VMB", "VME", "VSR", "PMI", "PHR", "UNK", "DVM", "VET"
    };

    /// <summary>
    /// Mapping from Banner department codes to Effort custodial departments.
    /// </summary>
    private static readonly Dictionary<string, string> BannerDeptMapping = new()
    {
        { "72030", "VME" },
        { "72035", "VSR" },
        { "72037", "APC" },
        { "72047", "VMB" },
        { "72057", "PMI" },
        { "72067", "PHR" }
    };

    public CourseService(
        EffortDbContext context,
        CoursesContext coursesContext,
        IEffortAuditService auditService,
        ILogger<CourseService> logger)
    {
        _context = context;
        _coursesContext = coursesContext;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<CourseDto>> GetCoursesAsync(int termCode, string? department = null, CancellationToken ct = default)
    {
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.TermCode == termCode);

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(c => c.CustDept == department);
        }

        // Get parent course IDs for all child courses (to determine which courses have parents)
        var childToParentMap = await _context.CourseRelationships
            .AsNoTracking()
            .ToDictionaryAsync(r => r.ChildCourseId, r => r.ParentCourseId, ct);

        var courses = await query
            .OrderBy(c => c.SubjCode)
            .ThenBy(c => c.CrseNumb)
            .ThenBy(c => c.SeqNumb)
            .ToListAsync(ct);

        return courses.Select(c => ToDto(c, childToParentMap.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<CourseDto?> GetCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        return course == null ? null : ToDto(course);
    }

    public async Task<List<BannerCourseDto>> SearchBannerCoursesAsync(int termCode, string? subjCode = null,
        string? crseNumb = null, string? crn = null, CancellationToken ct = default)
    {
        var termCodeStr = termCode.ToString();

        var query = _coursesContext.Baseinfos
            .AsNoTracking()
            .Where(b => b.BaseinfoTermCode == termCodeStr);

        if (!string.IsNullOrWhiteSpace(subjCode))
        {
            var upperSubjCode = subjCode.ToUpperInvariant();
            query = query.Where(b => b.BaseinfoSubjCode.Trim() == upperSubjCode);
        }

        if (!string.IsNullOrWhiteSpace(crseNumb))
        {
            var upperCrseNumb = crseNumb.ToUpperInvariant();
            query = query.Where(b => b.BaseinfoCrseNumb.Trim() == upperCrseNumb);
        }

        if (!string.IsNullOrWhiteSpace(crn))
        {
            query = query.Where(b => b.BaseinfoCrn.Trim() == crn.Trim());
        }

        var bannerCourses = await query
            .OrderBy(b => b.BaseinfoSubjCode)
            .ThenBy(b => b.BaseinfoCrseNumb)
            .ThenBy(b => b.BaseinfoSeqNumb)
            .Take(100)
            .ToListAsync(ct);

        // Check which courses are already imported
        var crns = bannerCourses.Select(b => b.BaseinfoCrn.Trim()).Distinct().ToList();
        var importedCourses = await _context.Courses
            .AsNoTracking()
            .Where(c => c.TermCode == termCode && crns.Contains(c.Crn))
            .Select(c => new { c.Crn, c.Units })
            .ToListAsync(ct);

        var importedByCrn = importedCourses
            .GroupBy(c => c.Crn.Trim())
            .ToDictionary(g => g.Key, g => g.Select(c => c.Units).ToList());

        return bannerCourses.Select(b =>
        {
            var crnTrimmed = b.BaseinfoCrn.Trim();
            var isFixed = b.BaseinfoUnitType == "F";
            var importedUnits = importedByCrn.GetValueOrDefault(crnTrimmed, new List<decimal>());

            return new BannerCourseDto
            {
                Crn = crnTrimmed,
                SubjCode = b.BaseinfoSubjCode.Trim(),
                CrseNumb = b.BaseinfoCrseNumb.Trim(),
                SeqNumb = b.BaseinfoSeqNumb.Trim(),
                Title = b.BaseinfoDescTitle ?? b.BaseinfoTitle,
                Enrollment = b.BaseinfoEnrollment,
                UnitType = b.BaseinfoUnitType,
                UnitLow = b.BaseinfoUnitLow,
                UnitHigh = b.BaseinfoUnitHigh,
                DeptCode = b.BaseinfoDeptCode.Trim(),
                AlreadyImported = isFixed && importedUnits.Count > 0,
                ImportedUnitValues = importedUnits
            };
        }).ToList();
    }

    public async Task<BannerCourseDto?> GetBannerCourseAsync(int termCode, string crn, CancellationToken ct = default)
    {
        var termCodeStr = termCode.ToString();
        var crnTrimmed = crn.Trim();

        var bannerCourse = await _coursesContext.Baseinfos
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BaseinfoTermCode == termCodeStr && b.BaseinfoCrn.Trim() == crnTrimmed, ct);

        if (bannerCourse == null)
        {
            return null;
        }

        // Check if already imported
        var importedUnits = await _context.Courses
            .AsNoTracking()
            .Where(c => c.TermCode == termCode && c.Crn.Trim() == crnTrimmed)
            .Select(c => c.Units)
            .ToListAsync(ct);

        var isFixed = bannerCourse.BaseinfoUnitType == "F";

        return new BannerCourseDto
        {
            Crn = crnTrimmed,
            SubjCode = bannerCourse.BaseinfoSubjCode.Trim(),
            CrseNumb = bannerCourse.BaseinfoCrseNumb.Trim(),
            SeqNumb = bannerCourse.BaseinfoSeqNumb.Trim(),
            Title = bannerCourse.BaseinfoDescTitle ?? bannerCourse.BaseinfoTitle,
            Enrollment = bannerCourse.BaseinfoEnrollment,
            UnitType = bannerCourse.BaseinfoUnitType,
            UnitLow = bannerCourse.BaseinfoUnitLow,
            UnitHigh = bannerCourse.BaseinfoUnitHigh,
            DeptCode = bannerCourse.BaseinfoDeptCode.Trim(),
            AlreadyImported = isFixed && importedUnits.Count > 0,
            ImportedUnitValues = importedUnits
        };
    }

    public async Task<bool> CourseExistsAsync(int termCode, string crn, decimal units, CancellationToken ct = default)
    {
        return await _context.Courses
            .AnyAsync(c => c.TermCode == termCode && c.Crn.Trim() == crn.Trim() && c.Units == units, ct);
    }

    public async Task<CourseDto> ImportCourseFromBannerAsync(ImportCourseRequest request, BannerCourseDto bannerCourse, CancellationToken ct = default)
    {
        // Determine units - controller has validated units are in range if variable
        var isVariable = bannerCourse.UnitType == "V";
        var units = isVariable && request.Units.HasValue
            ? request.Units.Value
            : bannerCourse.UnitLow;

        // Determine custodial department from Banner department mapping
        var custDept = GetCustodialDepartment(bannerCourse.DeptCode);

        // Create the course
        var course = new EffortCourse
        {
            Crn = bannerCourse.Crn,
            TermCode = request.TermCode,
            SubjCode = bannerCourse.SubjCode,
            CrseNumb = bannerCourse.CrseNumb,
            SeqNumb = bannerCourse.SeqNumb,
            Enrollment = bannerCourse.Enrollment,
            Units = units,
            CustDept = custDept
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(ct);

        _auditService.AddCourseChangeAudit(course.Id, course.TermCode, EffortAuditActions.CreateCourse,
            null,
            new
            {
                course.Crn,
                course.SubjCode,
                course.CrseNumb,
                course.SeqNumb,
                course.Enrollment,
                course.Units,
                course.CustDept
            });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Imported course {SubjCode} {CrseNumb}-{SeqNumb} (CRN: {Crn}) for term {TermCode}",
            course.SubjCode, course.CrseNumb, course.SeqNumb, course.Crn, course.TermCode);

        return ToDto(course);
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        var course = new EffortCourse
        {
            Crn = request.Crn.Trim().ToUpperInvariant(),
            TermCode = request.TermCode,
            SubjCode = request.SubjCode.Trim().ToUpperInvariant(),
            CrseNumb = request.CrseNumb.Trim().ToUpperInvariant(),
            SeqNumb = request.SeqNumb.Trim().ToUpperInvariant(),
            Enrollment = request.Enrollment,
            Units = request.Units,
            CustDept = request.CustDept.Trim().ToUpperInvariant()
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(ct);

        _auditService.AddCourseChangeAudit(course.Id, course.TermCode, EffortAuditActions.CreateCourse,
            null,
            new
            {
                course.Crn,
                course.SubjCode,
                course.CrseNumb,
                course.SeqNumb,
                course.Enrollment,
                course.Units,
                course.CustDept
            });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Created manual course {SubjCode} {CrseNumb}-{SeqNumb} (CRN: {Crn}) for term {TermCode}",
            course.SubjCode, course.CrseNumb, course.SeqNumb, course.Crn, course.TermCode);

        return ToDto(course);
    }

    public async Task<CourseDto?> UpdateCourseAsync(int courseId, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course == null)
        {
            return null;
        }

        // Validate custodial department
        if (!ValidCustDepts.Contains(request.CustDept.ToUpperInvariant()))
        {
            throw new ArgumentException($"Invalid custodial department: {request.CustDept}");
        }

        var oldValues = new
        {
            course.Enrollment,
            course.Units,
            course.CustDept
        };

        course.Enrollment = request.Enrollment;
        course.Units = request.Units;
        course.CustDept = request.CustDept.ToUpperInvariant();

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _auditService.AddCourseChangeAudit(course.Id, course.TermCode, EffortAuditActions.UpdateCourse,
            oldValues,
            new
            {
                course.Enrollment,
                course.Units,
                course.CustDept
            });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Updated course {CourseId} ({SubjCode} {CrseNumb})",
            courseId, course.SubjCode, course.CrseNumb);

        return ToDto(course);
    }

    public async Task<CourseDto?> UpdateCourseEnrollmentAsync(int courseId, int enrollment, CancellationToken ct = default)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course == null)
        {
            return null;
        }

        // Enforce R-course restriction: course number must end with 'R'
        if (!course.CrseNumb.Trim().EndsWith("R", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Course {course.SubjCode} {course.CrseNumb} is not an R-course. Only R-courses can be updated with this permission.");
        }

        var oldEnrollment = course.Enrollment;
        course.Enrollment = enrollment;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _auditService.AddCourseChangeAudit(course.Id, course.TermCode, EffortAuditActions.UpdateCourse,
            new { Enrollment = oldEnrollment },
            new { Enrollment = enrollment });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Updated R-course enrollment {CourseId} ({SubjCode} {CrseNumb}): {OldEnrollment} -> {NewEnrollment}",
            courseId, course.SubjCode, course.CrseNumb, oldEnrollment, enrollment);

        return ToDto(course);
    }

    public async Task<bool> DeleteCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .Include(c => c.Records)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null)
        {
            return false;
        }

        var courseInfo = new
        {
            course.Crn,
            course.SubjCode,
            course.CrseNumb,
            course.SeqNumb,
            course.Enrollment,
            course.Units,
            course.CustDept,
            RecordCount = course.Records.Count
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        // Delete associated effort records first (cascade)
        if (course.Records.Count > 0)
        {
            _context.Records.RemoveRange(course.Records);
            _logger.LogInformation("Deleted {RecordCount} effort records for course {CourseId}",
                course.Records.Count, courseId);
        }

        _context.Courses.Remove(course);

        _auditService.AddCourseChangeAudit(courseId, course.TermCode, EffortAuditActions.DeleteCourse,
            courseInfo,
            null);

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Deleted course {CourseId} ({SubjCode} {CrseNumb})",
            courseId, courseInfo.SubjCode, courseInfo.CrseNumb);

        return true;
    }

    public async Task<(bool CanDelete, int RecordCount)> CanDeleteCourseAsync(int courseId, CancellationToken ct = default)
    {
        var recordCount = await _context.Records
            .CountAsync(r => r.CourseId == courseId, ct);

        // Course can always be deleted, but we return the record count so the UI can warn the user
        return (true, recordCount);
    }

    public List<string> GetValidCustodialDepartments()
    {
        return ValidCustDepts.ToList();
    }

    public bool IsValidCustodialDepartment(string departmentCode)
    {
        return ValidCustDepts.Contains(departmentCode.ToUpperInvariant());
    }

    public string GetCustodialDepartmentForBannerCode(string bannerDeptCode)
    {
        return GetCustodialDepartment(bannerDeptCode);
    }

    /// <summary>
    /// Get the custodial department based on Banner department code.
    /// </summary>
    private static string GetCustodialDepartment(string bannerDeptCode)
    {
        if (BannerDeptMapping.TryGetValue(bannerDeptCode, out var custDept))
        {
            return custDept;
        }

        // Check if it's a valid SVM department code directly
        if (ValidCustDepts.Contains(bannerDeptCode.ToUpperInvariant()))
        {
            return bannerDeptCode.ToUpperInvariant();
        }

        return "UNK";
    }

    private static CourseDto ToDto(EffortCourse course, int? parentCourseId = null)
    {
        return new CourseDto
        {
            Id = course.Id,
            Crn = course.Crn.Trim(),
            TermCode = course.TermCode,
            SubjCode = course.SubjCode.Trim(),
            CrseNumb = course.CrseNumb.Trim(),
            SeqNumb = course.SeqNumb.Trim(),
            Enrollment = course.Enrollment,
            Units = course.Units,
            CustDept = course.CustDept.Trim(),
            ParentCourseId = parentCourseId
        };
    }
}
