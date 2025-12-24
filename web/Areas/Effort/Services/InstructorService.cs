using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for instructor-related operations in the Effort system.
/// </summary>
public class InstructorService : IInstructorService
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly AAUDContext _aaudContext;
    private readonly IEffortAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<InstructorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    private const string TitleCacheKey = "effort_title_lookup";

    /// <summary>
    /// Valid academic department codes.
    /// </summary>
    private static readonly List<DepartmentDto> Departments = new()
    {
        // Academic Departments
        new() { Code = "APC", Name = "Anatomy, Physiology & Cell Biology", Group = "Academic" },
        new() { Code = "PHR", Name = "Pathology, Microbiology & Immunology", Group = "Academic" },
        new() { Code = "PMI", Name = "Population Health & Reproduction", Group = "Academic" },
        new() { Code = "VMB", Name = "Molecular Biosciences", Group = "Academic" },
        new() { Code = "VME", Name = "Medicine & Epidemiology", Group = "Academic" },
        new() { Code = "VSR", Name = "Surgical & Radiological Sciences", Group = "Academic" },
        // Centers
        new() { Code = "WHC", Name = "Wildlife Health Center", Group = "Centers" },
        new() { Code = "OHI", Name = "One Health Institute", Group = "Centers" },
        new() { Code = "CCM", Name = "Center for Companion Animal Health", Group = "Centers" },
        new() { Code = "WIFSS", Name = "Western Institute for Food Safety", Group = "Centers" },
        new() { Code = "VGL", Name = "Veterinary Genetics Laboratory", Group = "Centers" },
        // Other
        new() { Code = "OTH", Name = "Other", Group = "Other" },
        new() { Code = "UNK", Name = "Unknown", Group = "Other" },
    };

    /// <summary>
    /// Academic department codes (priority for department assignment).
    /// </summary>
    private static readonly HashSet<string> AcademicDepts = new(StringComparer.OrdinalIgnoreCase)
    {
        "APC", "PHR", "PMI", "VMB", "VME", "VSR"
    };

    public InstructorService(
        EffortDbContext context,
        VIPERContext viperContext,
        AAUDContext aaudContext,
        IEffortAuditService auditService,
        IMapper mapper,
        ILogger<InstructorService> logger,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _context = context;
        _viperContext = viperContext;
        _aaudContext = aaudContext;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<List<PersonDto>> GetInstructorsAsync(int termCode, string? department = null, CancellationToken ct = default)
    {
        var query = _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode);

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(p => p.EffortDept == department);
        }

        var instructors = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(ct);

        var dtos = _mapper.Map<List<PersonDto>>(instructors);

        // Enrich with titles from dictionary database
        await EnrichWithTitlesAsync(dtos, ct);

        return dtos;
    }

    public async Task<PersonDto?> GetInstructorAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var instructor = await _context.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId && p.TermCode == termCode, ct);

        if (instructor == null) return null;

        var dto = _mapper.Map<PersonDto>(instructor);
        await EnrichWithTitlesAsync(new List<PersonDto> { dto }, ct);
        return dto;
    }

    public async Task<List<AaudPersonDto>> SearchPossibleInstructorsAsync(int termCode, string? searchTerm = null, CancellationToken ct = default)
    {
        // Enforce minimum search length server-side to prevent employee enumeration
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Trim().Length < 2)
        {
            return [];
        }

        // Get person IDs already in effort for this term
        var existingPersonIds = await _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode)
            .Select(p => p.PersonId)
            .ToListAsync(ct);

        // Search VIPER.users.Person for current employees not in effort
        var query = _viperContext.People
            .AsNoTracking()
            .Where(p => p.CurrentEmployee && !existingPersonIds.Contains(p.PersonId));

        var term = searchTerm.Trim().ToLower();
        query = query.Where(p =>
            p.LastName.ToLower().Contains(term) ||
            p.FirstName.ToLower().Contains(term) ||
            p.FullName.ToLower().Contains(term));

        var people = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(50) // Limit results for performance
            .ToListAsync(ct);

        // Get employee info from AAUD for department/title lookup
        var mothraIds = people.Select(p => p.MothraId).ToList();
        var employees = await _aaudContext.Employees
            .AsNoTracking()
            .Where(e => mothraIds.Contains(e.EmpPKey))
            .ToListAsync(ct);

        var employeeDict = employees.ToDictionary(e => e.EmpPKey, StringComparer.OrdinalIgnoreCase);

        return people.Select(p =>
        {
            employeeDict.TryGetValue(p.MothraId, out var emp);
            return new AaudPersonDto
            {
                PersonId = p.PersonId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                MiddleInitial = p.MiddleName?.Length > 0 ? p.MiddleName.Substring(0, 1) : null,
                EffortDept = DetermineDepartment(emp),
                TitleCode = emp?.EmpEffortTitleCode?.Trim() ?? emp?.EmpTeachingTitleCode?.Trim(),
                JobGroupId = null // Job group would need additional lookup if required
            };
        }).ToList();
    }

    public async Task<PersonDto> CreateInstructorAsync(CreateInstructorRequest request, CancellationToken ct = default)
    {
        // Check if instructor already exists for this term
        if (await InstructorExistsAsync(request.PersonId, request.TermCode, ct))
        {
            throw new InvalidOperationException($"Instructor with PersonId {request.PersonId} already exists for term {request.TermCode}");
        }

        // Look up person from VIPER.users.Person
        var person = await _viperContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == request.PersonId, ct);

        if (person == null)
        {
            throw new InvalidOperationException($"Person with PersonId {request.PersonId} not found");
        }

        // Look up employee info from AAUD
        var employee = await _aaudContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmpPKey == person.MothraId, ct);

        var dept = DetermineDepartment(employee) ?? "UNK";
        var titleCode = employee?.EmpEffortTitleCode?.Trim() ?? employee?.EmpTeachingTitleCode?.Trim() ?? "";

        var instructor = new EffortPerson
        {
            PersonId = request.PersonId,
            TermCode = request.TermCode,
            FirstName = person.FirstName,
            LastName = person.LastName,
            MiddleInitial = person.MiddleName?.Length > 0 ? person.MiddleName.Substring(0, 1) : null,
            EffortDept = dept,
            EffortTitleCode = titleCode,
            JobGroupId = null,
            PercentAdmin = 0,
            PercentClinical = null,
            VolunteerWos = null,
            ReportUnit = null,
            Title = null,
            AdminUnit = null,
            EffortVerified = null
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.Persons.Add(instructor);
        await _context.SaveChangesAsync(ct);

        _auditService.AddPersonChangeAudit(request.PersonId, request.TermCode, EffortAuditActions.CreatePerson,
            null,
            new
            {
                instructor.PersonId,
                instructor.FirstName,
                instructor.LastName,
                instructor.EffortDept,
                instructor.EffortTitleCode
            });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Created instructor: {PersonId} ({LastName}, {FirstName}) for term {TermCode}",
            instructor.PersonId, instructor.LastName, instructor.FirstName, instructor.TermCode);

        return _mapper.Map<PersonDto>(instructor);
    }

    public async Task<PersonDto?> UpdateInstructorAsync(int personId, int termCode, UpdateInstructorRequest request, CancellationToken ct = default)
    {
        var instructor = await _context.Persons.FirstOrDefaultAsync(p => p.PersonId == personId && p.TermCode == termCode, ct);
        if (instructor == null)
        {
            return null;
        }

        // Validate department
        if (!IsValidDepartment(request.EffortDept))
        {
            throw new ArgumentException($"Invalid department: {request.EffortDept}");
        }

        var oldValues = new
        {
            instructor.EffortDept,
            instructor.EffortTitleCode,
            instructor.JobGroupId,
            instructor.ReportUnit,
            instructor.VolunteerWos
        };

        instructor.EffortDept = request.EffortDept.ToUpperInvariant();
        instructor.EffortTitleCode = request.EffortTitleCode?.Trim() ?? "";
        instructor.JobGroupId = request.JobGroupId?.Trim();
        instructor.ReportUnit = request.ReportUnits != null && request.ReportUnits.Count > 0
            ? string.Join(",", request.ReportUnits)
            : null;
        instructor.VolunteerWos = request.VolunteerWos ? (byte)1 : null;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _auditService.AddPersonChangeAudit(personId, termCode, EffortAuditActions.UpdatePerson,
            oldValues,
            new
            {
                instructor.EffortDept,
                instructor.EffortTitleCode,
                instructor.JobGroupId,
                instructor.ReportUnit,
                instructor.VolunteerWos
            });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Updated instructor: {PersonId} for term {TermCode}", personId, termCode);

        return _mapper.Map<PersonDto>(instructor);
    }

    public async Task<bool> DeleteInstructorAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var instructor = await _context.Persons
            .Include(p => p.Records)
            .FirstOrDefaultAsync(p => p.PersonId == personId && p.TermCode == termCode, ct);

        if (instructor == null)
        {
            return false;
        }

        var instructorInfo = new
        {
            instructor.PersonId,
            instructor.FirstName,
            instructor.LastName,
            instructor.EffortDept,
            RecordCount = instructor.Records.Count
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        // Delete associated effort records first (cascade)
        if (instructor.Records.Count > 0)
        {
            _context.Records.RemoveRange(instructor.Records);
            _logger.LogInformation("Deleted {RecordCount} effort records for instructor {PersonId}",
                instructor.Records.Count, personId);
        }

        _context.Persons.Remove(instructor);

        _auditService.AddPersonChangeAudit(personId, termCode, EffortAuditActions.DeleteInstructor,
            instructorInfo,
            null);

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Deleted instructor: {PersonId} ({LastName}, {FirstName}) for term {TermCode}",
            personId, instructorInfo.LastName, instructorInfo.FirstName, termCode);

        return true;
    }

    public async Task<(bool CanDelete, int RecordCount)> CanDeleteInstructorAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var recordCount = await _context.Records
            .CountAsync(r => r.PersonId == personId && r.TermCode == termCode, ct);

        // Instructors can always be deleted, but we return the record count for UI warning
        return (true, recordCount);
    }

    public async Task<bool> InstructorExistsAsync(int personId, int termCode, CancellationToken ct = default)
    {
        return await _context.Persons
            .AnyAsync(p => p.PersonId == personId && p.TermCode == termCode, ct);
    }

    public async Task<string?> ResolveInstructorDepartmentAsync(int personId, CancellationToken ct = default)
    {
        var person = await _viperContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, ct);

        if (person == null)
        {
            return null;
        }

        var employee = await _aaudContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmpPKey == person.MothraId, ct);

        return DetermineDepartment(employee) ?? "UNK";
    }

    public List<DepartmentDto> GetDepartments()
    {
        return Departments.ToList();
    }

    public List<string> GetValidDepartments()
    {
        return Departments.Select(d => d.Code).ToList();
    }

    public bool IsValidDepartment(string departmentCode)
    {
        return Departments.Any(d => string.Equals(d.Code, departmentCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<ReportUnitDto>> GetReportUnitsAsync(CancellationToken ct = default)
    {
        var units = await _context.ReportUnits
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.SortOrder ?? 0)
            .ThenBy(u => u.UnitName)
            .ToListAsync(ct);

        return units.Select(u => new ReportUnitDto
        {
            Abbrev = u.UnitCode,
            Unit = u.UnitName
        }).ToList();
    }

    /// <summary>
    /// Determine the effort department from AAUD employee data.
    /// Priority: Effort dept, then teaching dept, then home dept.
    /// Only use if it's an academic SVM department.
    /// </summary>
    private static string? DetermineDepartment(Viper.Models.AAUD.Employee? employee)
    {
        if (employee == null)
        {
            return null;
        }

        // Check effort dept first
        if (!string.IsNullOrWhiteSpace(employee.EmpEffortHomeDept) &&
            AcademicDepts.Contains(employee.EmpEffortHomeDept.Trim()))
        {
            return employee.EmpEffortHomeDept.Trim().ToUpperInvariant();
        }

        // Check teaching dept
        if (!string.IsNullOrWhiteSpace(employee.EmpTeachingHomeDept) &&
            AcademicDepts.Contains(employee.EmpTeachingHomeDept.Trim()))
        {
            return employee.EmpTeachingHomeDept.Trim().ToUpperInvariant();
        }

        // Check home dept
        if (!string.IsNullOrWhiteSpace(employee.EmpHomeDept) &&
            AcademicDepts.Contains(employee.EmpHomeDept.Trim()))
        {
            return employee.EmpHomeDept.Trim().ToUpperInvariant();
        }

        // Check alt dept
        if (!string.IsNullOrWhiteSpace(employee.EmpAltDeptCode) &&
            AcademicDepts.Contains(employee.EmpAltDeptCode.Trim()))
        {
            return employee.EmpAltDeptCode.Trim().ToUpperInvariant();
        }

        return null;
    }

    /// <summary>
    /// Enriches instructor DTOs with titles from the dictionary database.
    /// Matches EffortTitleCode to dvtTitle_Code (with zero-padding) to get dvtTitle_Abbrv.
    /// </summary>
    /// <remarks>
    /// Uses raw SQL because the dictionary database is on the same SQL Server instance
    /// but in a different database. EF Core contexts are tied to a single database,
    /// so we use cross-database queries via [dictionary].[dbo].[dvtTitle].
    /// Results are cached since title codes are static reference data.
    /// </remarks>
    private async Task EnrichWithTitlesAsync(List<PersonDto> instructors, CancellationToken ct)
    {
        if (instructors.Count == 0) return;

        var titleLookup = await GetTitleLookupAsync(ct);
        if (titleLookup == null) return;

        foreach (var instructor in instructors)
        {
            if (string.IsNullOrWhiteSpace(instructor.EffortTitleCode)) continue;

            var paddedCode = instructor.EffortTitleCode.PadLeft(6, '0');
            if (titleLookup.TryGetValue(paddedCode, out var title))
            {
                instructor.Title = title;
            }
        }
    }

    /// <summary>
    /// Gets the title code to abbreviation lookup dictionary, loading from database if not cached.
    /// </summary>
    private async Task<Dictionary<string, string>?> GetTitleLookupAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue<Dictionary<string, string>>(TitleCacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var connectionString = _configuration.GetConnectionString("VIPER");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("VIPER connection string not found, cannot load title lookup");
            return null;
        }

        try
        {
            var titleLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            var query = @"
                SELECT DISTINCT
                    dvtTitle_Code AS TitleCode,
                    dvtTitle_Abbrv AS TitleAbbrev
                FROM [dictionary].[dbo].[dvtTitle]
                WHERE dvtTitle_Code IS NOT NULL
                    AND dvtTitle_Abbrv IS NOT NULL";

            await using var cmd = new SqlCommand(query, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var code = reader["TitleCode"]?.ToString();
                var abbrev = reader["TitleAbbrev"]?.ToString();
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(abbrev))
                {
                    titleLookup[code] = abbrev;
                }
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(TitleCacheKey, titleLookup, cacheOptions);

            _logger.LogInformation("Loaded {Count} title codes from dictionary database", titleLookup.Count);

            return titleLookup;
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Failed to load title lookup from dictionary database");
            return null;
        }
    }

    public async Task<List<InstructorEffortRecordDto>> GetInstructorEffortRecordsAsync(
        int personId, int termCode, CancellationToken ct = default)
    {
        var records = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Include(r => r.RoleNavigation)
            .Where(r => r.PersonId == personId && r.TermCode == termCode)
            .OrderBy(r => r.RoleNavigation.SortOrder)
            .ThenBy(r => r.Course.SubjCode)
            .ThenBy(r => r.Course.CrseNumb)
            .ThenBy(r => r.Course.SeqNumb)
            .ToListAsync(ct);

        return records.Select(r => new InstructorEffortRecordDto
        {
            Id = r.Id,
            CourseId = r.CourseId,
            PersonId = r.PersonId,
            TermCode = r.TermCode,
            SessionType = r.SessionType,
            Role = r.Role,
            RoleDescription = r.RoleNavigation?.Description ?? string.Empty,
            Hours = r.Hours,
            Weeks = r.Weeks,
            Crn = r.Crn,
            ModifiedDate = r.ModifiedDate,
            Course = new CourseDto
            {
                Id = r.Course.Id,
                Crn = r.Course.Crn,
                TermCode = r.Course.TermCode,
                SubjCode = r.Course.SubjCode,
                CrseNumb = r.Course.CrseNumb,
                SeqNumb = r.Course.SeqNumb,
                Enrollment = r.Course.Enrollment,
                Units = r.Course.Units,
                CustDept = r.Course.CustDept
            }
        }).ToList();
    }
}
