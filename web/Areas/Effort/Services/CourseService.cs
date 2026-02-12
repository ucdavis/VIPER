using System.Data;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for course-related operations in the Effort system.
/// </summary>
public class CourseService : ICourseService
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditService;
    private readonly ICourseClassificationService _classificationService;
    private readonly ILogger<CourseService> _logger;

    /// <summary>
    /// Valid custodial department codes for the Effort system.
    /// </summary>
    private static readonly List<string> ValidCustDepts = new()
    {
        "APC", "VMB", "VME", "VSR", "PMI", "PHR", "UNK", "DVM", "VET"
    };

    /// <summary>
    /// Mapping from Banner custodial department codes to Effort custodial departments.
    /// Banner codes may be stored with or without leading zeros (e.g., "72030" or "072030").
    /// The GetCustodialDepartment method normalizes input by padding to 6 digits.
    /// </summary>
    private static readonly Dictionary<string, string> BannerDeptMapping = new()
    {
        { "072030", "VME" },
        { "072035", "VSR" },
        { "072037", "APC" },
        { "072047", "VMB" },
        { "072057", "PMI" },
        { "072067", "PHR" }
    };

    public CourseService(
        EffortDbContext context,
        IEffortAuditService auditService,
        ICourseClassificationService classificationService,
        ILogger<CourseService> logger)
    {
        _context = context;
        _auditService = auditService;
        _classificationService = classificationService;
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

        // Get parent course IDs for child courses in this term only
        var childToParentMap = await _context.CourseRelationships
            .AsNoTracking()
            .Where(r => _context.Courses.Any(c => c.Id == r.ParentCourseId && c.TermCode == termCode))
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

        if (course == null) return null;

        // Check if this course is a child in any relationship
        var parentCourseId = await _context.CourseRelationships
            .AsNoTracking()
            .Where(r => r.ChildCourseId == courseId)
            .Select(r => (int?)r.ParentCourseId)
            .FirstOrDefaultAsync(ct);

        return ToDto(course, parentCourseId);
    }

    public async Task<List<BannerCourseDto>> SearchBannerCoursesAsync(int termCode, string? subjCode = null,
        string? crseNumb = null, string? seqNumb = null, string? crn = null, CancellationToken ct = default)
    {
        // Validate that at least one search parameter is provided
        if (string.IsNullOrWhiteSpace(subjCode) && string.IsNullOrWhiteSpace(crseNumb) &&
            string.IsNullOrWhiteSpace(seqNumb) && string.IsNullOrWhiteSpace(crn))
        {
            throw new ArgumentException("At least one search parameter (subjCode, crseNumb, seqNumb, or crn) is required");
        }

        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var bannerCourses = new List<BannerCourseDto>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_search_banner_courses]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode.ToString());
        command.Parameters.AddWithValue("@SubjCode", string.IsNullOrWhiteSpace(subjCode) ? DBNull.Value : subjCode.ToUpperInvariant());
        command.Parameters.AddWithValue("@CrseNumb", string.IsNullOrWhiteSpace(crseNumb) ? DBNull.Value : crseNumb.ToUpperInvariant());
        command.Parameters.AddWithValue("@SeqNumb", string.IsNullOrWhiteSpace(seqNumb) ? DBNull.Value : seqNumb.ToUpperInvariant());
        command.Parameters.AddWithValue("@Crn", string.IsNullOrWhiteSpace(crn) ? DBNull.Value : crn.Trim());

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            bannerCourses.Add(new BannerCourseDto
            {
                Crn = reader.GetString(0).Trim(),
                SubjCode = reader.GetString(1).Trim(),
                CrseNumb = reader.GetString(2).Trim(),
                SeqNumb = reader.GetString(3).Trim(),
                Title = await reader.IsDBNullAsync(4, ct) ? null : reader.GetString(4),
                Enrollment = Convert.ToInt32(reader.GetValue(5)),
                UnitType = reader.GetString(6),
                UnitLow = Convert.ToDecimal(reader.GetValue(7)),
                UnitHigh = Convert.ToDecimal(reader.GetValue(8)),
                DeptCode = reader.GetString(9).Trim()
            });
        }

        // Check which courses are already imported
        var crns = bannerCourses.Select(b => b.Crn).Distinct().ToList();
        if (crns.Count > 0)
        {
            var importedCourses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TermCode == termCode && crns.Contains(c.Crn))
                .Select(c => new { c.Crn, c.Units })
                .ToListAsync(ct);

            var importedByCrn = importedCourses
                .GroupBy(c => c.Crn.Trim())
                .ToDictionary(g => g.Key, g => g.Select(c => c.Units).ToList());

            foreach (var course in bannerCourses)
            {
                var isFixed = course.UnitType == "F";
                var importedUnits = importedByCrn.GetValueOrDefault(course.Crn, new List<decimal>());
                course.AlreadyImported = isFixed && importedUnits.Count > 0;
                course.ImportedUnitValues = importedUnits;
            }
        }

        return bannerCourses;
    }

    public async Task<BannerCourseDto?> GetBannerCourseAsync(int termCode, string crn, CancellationToken ct = default)
    {
        // Use the search SP with CRN filter to get a single course from Banner
        var courses = await SearchBannerCoursesAsync(termCode, crn: crn.Trim(), ct: ct);
        return courses.FirstOrDefault();
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

        // Determine custodial department - check subject code first, then fall back to dept code mapping
        // For SVM courses, the subject code (e.g., "DVM", "VME", "PHR") often matches the custodial department
        var custDept = GetCustodialDepartment(bannerCourse.SubjCode, bannerCourse.DeptCode);

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
            LogSanitizer.SanitizeId(course.SubjCode), LogSanitizer.SanitizeId(course.CrseNumb), LogSanitizer.SanitizeId(course.SeqNumb), LogSanitizer.SanitizeId(course.Crn), course.TermCode);

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
            LogSanitizer.SanitizeId(course.SubjCode), LogSanitizer.SanitizeId(course.CrseNumb), LogSanitizer.SanitizeId(course.SeqNumb), LogSanitizer.SanitizeId(course.Crn), course.TermCode);

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
            courseId, LogSanitizer.SanitizeId(course.SubjCode), LogSanitizer.SanitizeId(course.CrseNumb));

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
            courseId, LogSanitizer.SanitizeId(course.SubjCode), LogSanitizer.SanitizeId(course.CrseNumb), oldEnrollment, enrollment);

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
            courseId, LogSanitizer.SanitizeId(courseInfo.SubjCode), LogSanitizer.SanitizeId(courseInfo.CrseNumb));

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
        return GetCustodialDepartment(null, bannerDeptCode);
    }

    /// <summary>
    /// Get the custodial department based on subject code and/or Banner department code.
    /// For SVM courses, the subject code (e.g., "DVM", "VME") often IS the custodial department.
    /// Falls back to department code mapping for non-SVM subject codes.
    /// </summary>
    private static string GetCustodialDepartment(string? subjCode, string bannerDeptCode)
    {
        // First check if subject code is a valid SVM department (most common case for SVM courses)
        if (!string.IsNullOrWhiteSpace(subjCode))
        {
            var trimmedSubj = subjCode.Trim().ToUpperInvariant();
            if (ValidCustDepts.Contains(trimmedSubj))
            {
                return trimmedSubj;
            }
        }

        // Then check if dept code is already a valid SVM department code
        var trimmed = bannerDeptCode.Trim();
        if (ValidCustDepts.Contains(trimmed.ToUpperInvariant()))
        {
            return trimmed.ToUpperInvariant();
        }

        // Try to look up by numeric Banner code - normalize to 6 digits with leading zero
        var normalizedCode = trimmed.PadLeft(6, '0');
        if (BannerDeptMapping.TryGetValue(normalizedCode, out var custDept))
        {
            return custDept;
        }

        return "UNK";
    }

    private CourseDto ToDto(EffortCourse course, int? parentCourseId = null)
    {
        var dto = new CourseDto
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
        return dto.WithClassification(_classificationService.Classify(course));
    }

    /// <inheritdoc />
    public async Task<ImportCourseForSelfResult> ImportCourseForSelfAsync(int termCode, string crn, decimal? units = null, CancellationToken ct = default)
    {
        var crnTrimmed = crn.Trim();

        // First, check if course already exists in Effort (idempotent check)
        var existingCourse = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TermCode == termCode && c.Crn.Trim() == crnTrimmed, ct);

        if (existingCourse != null)
        {
            // Course already imported - if units specified and doesn't match, check for variable-unit match
            if (units.HasValue && existingCourse.Units != units.Value)
            {
                var requestedUnits = units.Value;
                existingCourse = await _context.Courses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.TermCode == termCode
                                           && c.Crn.Trim() == crnTrimmed
                                           && c.Units == requestedUnits, ct);

                if (existingCourse == null)
                {
                    // For variable-unit courses, we could create a new record with different units
                    // But for self-import, we'll just use the existing course
                    existingCourse = await _context.Courses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.TermCode == termCode && c.Crn.Trim() == crnTrimmed, ct);
                }
            }

            if (existingCourse != null)
            {
                _logger.LogDebug("Course {Crn} already exists for term {TermCode}", LogSanitizer.SanitizeId(crnTrimmed), termCode);
                return ImportCourseForSelfResult.SuccessExisting(ToDto(existingCourse));
            }
        }

        // Fetch course from Banner
        var bannerCourse = await GetBannerCourseAsync(termCode, crnTrimmed, ct);
        if (bannerCourse == null)
        {
            return ImportCourseForSelfResult.Failure($"Course with CRN {crnTrimmed} not found in Banner for term {termCode}");
        }

        // Determine units - require units for variable-unit courses
        var isVariable = bannerCourse.UnitType == "V";
        if (isVariable && !units.HasValue)
        {
            return ImportCourseForSelfResult.Failure("Units are required for variable-unit courses");
        }
        var finalUnits = isVariable ? units!.Value : bannerCourse.UnitLow;

        // Validate units for variable-unit courses
        if (isVariable && (finalUnits < bannerCourse.UnitLow || finalUnits > bannerCourse.UnitHigh))
        {
            return ImportCourseForSelfResult.Failure(
                $"Units {finalUnits} must be between {bannerCourse.UnitLow} and {bannerCourse.UnitHigh}");
        }

        // Check if course with these units already exists (for variable-unit courses)
        if (await CourseExistsAsync(termCode, crnTrimmed, finalUnits, ct))
        {
            var existingWithUnits = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TermCode == termCode
                                       && c.Crn.Trim() == crnTrimmed
                                       && c.Units == finalUnits, ct);
            if (existingWithUnits != null)
            {
                return ImportCourseForSelfResult.SuccessExisting(ToDto(existingWithUnits));
            }
        }

        // Import the course
        var importRequest = new ImportCourseRequest { TermCode = termCode, Crn = crnTrimmed, Units = finalUnits };
        var importedCourse = await ImportCourseFromBannerAsync(importRequest, bannerCourse, ct);

        _logger.LogInformation("Self-imported course {Crn} for term {TermCode}",
            LogSanitizer.SanitizeId(crnTrimmed), termCode);

        return ImportCourseForSelfResult.SuccessNew(importedCourse);
    }

    /// <inheritdoc />
    public async Task<List<CourseEffortRecordDto>> GetCourseEffortAsync(int courseId, CancellationToken ct = default)
    {
        var records = await _context.Records
            .AsNoTracking()
            .Include(r => r.Person)
            .Include(r => r.RoleNavigation)
            .Include(r => r.EffortTypeNavigation)
            .Where(r => r.CourseId == courseId)
            .OrderBy(r => r.Person.LastName)
            .ThenBy(r => r.Person.FirstName)
            .ThenBy(r => r.RoleNavigation.SortOrder)
            .ThenBy(r => r.EffortTypeNavigation.Description)
            .ToListAsync(ct);

        return records.Select(r => new CourseEffortRecordDto
        {
            EffortId = r.Id,
            PersonId = r.PersonId,
            InstructorName = $"{r.Person.LastName}, {r.Person.FirstName}",
            EffortTypeId = r.EffortTypeId,
            EffortTypeDescription = r.EffortTypeNavigation?.Description ?? string.Empty,
            RoleId = r.RoleId,
            RoleDescription = r.RoleNavigation?.Description ?? string.Empty,
            Hours = r.Hours,
            Weeks = r.Weeks,
            Notes = r.Notes,
            ModifiedDate = r.ModifiedDate
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<PossibleCourseInstructorsDto> GetPossibleInstructorsForCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null)
        {
            return new PossibleCourseInstructorsDto();
        }

        // Get personIds who already have effort on this course
        var existingPersonIds = await _context.Records
            .AsNoTracking()
            .Where(r => r.CourseId == courseId)
            .Select(r => r.PersonId)
            .Distinct()
            .ToListAsync(ct);

        // Get all instructors for the term, excluding guest accounts
        var allInstructors = await _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == course.TermCode
                        && p.FirstName != EffortConstants.GuestInstructorFirstName)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(ct);

        var existingInstructors = allInstructors
            .Where(p => existingPersonIds.Contains(p.PersonId))
            .Select(MapToInstructorOption)
            .ToList();

        var otherInstructors = allInstructors
            .Where(p => !existingPersonIds.Contains(p.PersonId))
            .Select(MapToInstructorOption)
            .ToList();

        return new PossibleCourseInstructorsDto
        {
            ExistingInstructors = existingInstructors,
            OtherInstructors = otherInstructors
        };
    }

    private static CourseInstructorOptionDto MapToInstructorOption(EffortPerson person)
    {
        return new CourseInstructorOptionDto
        {
            PersonId = person.PersonId,
            FirstName = person.FirstName,
            LastName = person.LastName,
            EffortDept = person.EffortDept?.Trim() ?? string.Empty
        };
    }
}
