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
    private const string DeptSimpleNameCacheKey = "effort_dept_simple_name_lookup";

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

    public async Task<List<PersonDto>> GetInstructorsByDepartmentsAsync(int termCode, IReadOnlyList<string> departments, CancellationToken ct = default)
    {
        if (departments.Count == 0)
        {
            return [];
        }

        var instructors = await _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode && departments.Contains(p.EffortDept))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(ct);

        var dtos = _mapper.Map<List<PersonDto>>(instructors);

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
        // Need to go through the Ids table to map MothraId -> pKey -> Employee
        var mothraIds = people.Select(p => p.MothraId).ToList();
        var termCodeStr = termCode.ToString();

        // Look up pKeys from Ids table using MothraId
        var idsRecords = await _aaudContext.Ids
            .AsNoTracking()
            .Where(i => i.IdsTermCode == termCodeStr && i.IdsMothraid != null && mothraIds.Contains(i.IdsMothraid))
            .ToListAsync(ct);

        // Build MothraId -> pKey mapping
        var mothraIdToPKey = idsRecords
            .Where(i => i.IdsMothraid != null)
            .ToDictionary(i => i.IdsMothraid!, i => i.IdsPKey, StringComparer.OrdinalIgnoreCase);

        // Get employees using pKeys
        var pKeys = mothraIdToPKey.Values.ToList();
        var employees = await _aaudContext.Employees
            .AsNoTracking()
            .Where(e => e.EmpTermCode == termCodeStr && pKeys.Contains(e.EmpPKey))
            .ToListAsync(ct);

        var employeeDict = employees.ToDictionary(e => e.EmpPKey, StringComparer.OrdinalIgnoreCase);

        // Get title lookup for human-readable title names
        var titleLookup = await GetTitleLookupAsync(ct);

        // Get department simple name lookup (raw code like "072030" -> simple name like "VME")
        var deptSimpleNameLookup = await GetDepartmentSimpleNameLookupAsync(ct);

        // Build simple department code -> display name lookup (e.g., "VME" -> "Medicine & Epidemiology")
        var deptDisplayNameLookup = Departments.ToDictionary(d => d.Code, d => d.Name, StringComparer.OrdinalIgnoreCase);

        // Batch-fetch job departments for all people (matches legacy jobs query)
        var validMothraIds = mothraIds.Where(m => !string.IsNullOrEmpty(m)).ToList();
        var jobDeptsByMothraId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (validMothraIds.Count > 0)
        {
            var jobData = await _aaudContext.Jobs
                .AsNoTracking()
                .Where(job => job.JobTermCode == termCodeStr)
                .Join(
                    _aaudContext.Ids.Where(i => i.IdsTermCode == termCodeStr && i.IdsMothraid != null && validMothraIds.Contains(i.IdsMothraid)),
                    job => job.JobPKey,
                    ids => ids.IdsPKey,
                    (job, ids) => new { ids.IdsMothraid, job.JobDepartmentCode })
                .ToListAsync(ct);

            jobDeptsByMothraId = jobData
                .Where(item => item.IdsMothraid != null)
                .GroupBy(item => item.IdsMothraid!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(item => item.JobDepartmentCode).ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        return people.Select(p =>
        {
            Viper.Models.AAUD.Employee? emp = null;
            if (mothraIdToPKey.TryGetValue(p.MothraId, out var pKey))
            {
                employeeDict.TryGetValue(pKey, out emp);
            }

            var deptCode = DetermineDepartmentFromJobs(p.MothraId, emp, jobDeptsByMothraId, deptSimpleNameLookup);
            var titleCode = emp?.EmpEffortTitleCode?.Trim() ?? emp?.EmpTeachingTitleCode?.Trim();

            // Look up human-readable names
            string? deptName = null;
            if (deptCode != null && deptDisplayNameLookup.TryGetValue(deptCode, out var name))
            {
                deptName = name;
            }

            string? title = null;
            if (titleCode != null && titleLookup != null)
            {
                var paddedCode = titleCode.PadLeft(6, '0');
                titleLookup.TryGetValue(paddedCode, out title);
            }

            return new AaudPersonDto
            {
                PersonId = p.PersonId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                MiddleInitial = p.MiddleName?.Length > 0 ? p.MiddleName.Substring(0, 1) : null,
                EffortDept = deptCode,
                DeptName = deptName,
                TitleCode = titleCode,
                Title = title,
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

        // Look up employee info from AAUD through Ids table (MothraId -> pKey -> Employee)
        var termCodeStr = request.TermCode.ToString();
        Viper.Models.AAUD.Id? idsRecord = null;
        if (!string.IsNullOrEmpty(person.MothraId))
        {
            idsRecord = await _aaudContext.Ids
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.IdsTermCode == termCodeStr && i.IdsMothraid == person.MothraId, ct);
        }

        Viper.Models.AAUD.Employee? employee = null;
        if (idsRecord != null)
        {
            employee = await _aaudContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmpTermCode == termCodeStr && e.EmpPKey == idsRecord.IdsPKey, ct);
        }

        // Get department simple name lookup to convert raw codes to simple names
        var deptSimpleNameLookup = await GetDepartmentSimpleNameLookupAsync(ct);
        var dept = await DetermineDepartmentAsync(person.MothraId, request.TermCode, employee, deptSimpleNameLookup, ct) ?? "UNK";
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

        // Validate department - allow known departments or keeping the current one (legacy data may have non-standard departments)
        var isCurrentDept = string.Equals(instructor.EffortDept, request.EffortDept, StringComparison.OrdinalIgnoreCase);
        if (!isCurrentDept && !IsValidDepartment(request.EffortDept))
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
        instructor.VolunteerWos = request.VolunteerWos ? (byte)1 : (byte)0;

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

    public async Task<string?> ResolveInstructorDepartmentAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var person = await _viperContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, ct);

        if (person == null)
        {
            return null;
        }

        // Look up employee info from AAUD through Ids table (MothraId -> pKey -> Employee)
        var termCodeStr = termCode.ToString();
        Viper.Models.AAUD.Id? idsRecord = null;
        if (!string.IsNullOrEmpty(person.MothraId))
        {
            idsRecord = await _aaudContext.Ids
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.IdsTermCode == termCodeStr && i.IdsMothraid == person.MothraId, ct);
        }

        Viper.Models.AAUD.Employee? employee = null;
        if (idsRecord != null)
        {
            employee = await _aaudContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmpTermCode == termCodeStr && e.EmpPKey == idsRecord.IdsPKey, ct);
        }

        // Get department simple name lookup to convert raw codes to simple names
        var deptSimpleNameLookup = await GetDepartmentSimpleNameLookupAsync(ct);
        return await DetermineDepartmentAsync(person.MothraId, termCode, employee, deptSimpleNameLookup, ct) ?? "UNK";
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
    /// Determine the effort department from AAUD data.
    /// Priority: 1) Special overrides, 2) First academic dept from jobs, 3) Employee fields, 4) Fallback.
    /// </summary>
    /// <remarks>
    /// Matches legacy Instructor.cfc logic (lines 61-104):
    /// - First queries AAUD jobs table for academic departments
    /// - Then falls back to employee-level fields (effort/home/alt dept)
    /// - Includes hardcoded overrides for special cases
    /// </remarks>
    private async Task<string?> DetermineDepartmentAsync(
        string? mothraId,
        int termCode,
        Viper.Models.AAUD.Employee? employee,
        Dictionary<string, string>? deptSimpleNameLookup,
        CancellationToken ct = default)
    {
        // Mison override - he only has a VMTH job, but needs his effort recorded to VSR
        if (mothraId == "02493928")
        {
            return "VSR";
        }

        // Helper to resolve raw code to simple name
        string? ToSimpleName(string? rawCode)
        {
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                return null;
            }
            var trimmed = rawCode.Trim();
            // If lookup available, use it; otherwise return raw value (fallback for simple codes)
            if (deptSimpleNameLookup != null && deptSimpleNameLookup.TryGetValue(trimmed, out var simpleName))
            {
                return simpleName;
            }
            // If raw code is already a valid department code (academic or center), return it
            if (Departments.Any(d => string.Equals(d.Code, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return trimmed.ToUpperInvariant();
            }
            return null;
        }

        // Step 1: Check AAUD jobs for first academic department (matches legacy qInstructorJobs query)
        if (!string.IsNullOrEmpty(mothraId))
        {
            var termCodeStr = termCode.ToString();
            var jobDepts = await _aaudContext.Jobs
                .AsNoTracking()
                .Where(job => job.JobTermCode == termCodeStr)
                .Join(
                    _aaudContext.Ids.Where(i => i.IdsTermCode == termCodeStr && i.IdsMothraid == mothraId),
                    job => job.JobPKey,
                    ids => ids.IdsPKey,
                    (job, ids) => job.JobDepartmentCode)
                .ToListAsync(ct);

            var firstAcademicDept = jobDepts
                .Select(ToSimpleName)
                .FirstOrDefault(simpleName => simpleName != null && AcademicDepts.Contains(simpleName));

            if (firstAcademicDept != null)
            {
                return firstAcademicDept.ToUpperInvariant();
            }
        }

        // Step 2: Fall back to employee fields (matches legacy qDetails checks)
        if (employee == null)
        {
            return null;
        }

        // Check effort dept first
        var effortDeptSimple = ToSimpleName(employee.EmpEffortHomeDept);
        if (effortDeptSimple != null && AcademicDepts.Contains(effortDeptSimple))
        {
            return effortDeptSimple.ToUpperInvariant();
        }

        // Check home dept
        var homeDeptSimple = ToSimpleName(employee.EmpHomeDept);
        if (homeDeptSimple != null && AcademicDepts.Contains(homeDeptSimple))
        {
            return homeDeptSimple.ToUpperInvariant();
        }

        // Check alt dept
        var altDeptSimple = ToSimpleName(employee.EmpAltDeptCode);
        if (altDeptSimple != null && AcademicDepts.Contains(altDeptSimple))
        {
            return altDeptSimple.ToUpperInvariant();
        }

        // No academic dept found - fall back to effort dept even if non-academic (matches legacy line 97)
        if (effortDeptSimple != null)
        {
            return effortDeptSimple.ToUpperInvariant();
        }

        return null;
    }

    /// <summary>
    /// Determine the effort department from AAUD data using pre-fetched job departments.
    /// Used by batch operations to avoid N+1 queries.
    /// </summary>
    private static string? DetermineDepartmentFromJobs(
        string? mothraId,
        Viper.Models.AAUD.Employee? employee,
        Dictionary<string, List<string>>? jobDeptsByMothraId,
        Dictionary<string, string>? deptSimpleNameLookup)
    {
        // Mison override - he only has a VMTH job, but needs his effort recorded to VSR
        if (mothraId == "02493928")
        {
            return "VSR";
        }

        // Helper to resolve raw code to simple name
        string? ToSimpleName(string? rawCode)
        {
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                return null;
            }
            var trimmed = rawCode.Trim();
            if (deptSimpleNameLookup != null && deptSimpleNameLookup.TryGetValue(trimmed, out var simpleName))
            {
                return simpleName;
            }
            if (Departments.Any(d => string.Equals(d.Code, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return trimmed.ToUpperInvariant();
            }
            return null;
        }

        // Step 1: Check jobs for first academic department
        if (!string.IsNullOrEmpty(mothraId) && jobDeptsByMothraId != null &&
            jobDeptsByMothraId.TryGetValue(mothraId, out var jobDepts))
        {
            var firstAcademicDept = jobDepts
                .Select(ToSimpleName)
                .FirstOrDefault(simpleName => simpleName != null && AcademicDepts.Contains(simpleName));

            if (firstAcademicDept != null)
            {
                return firstAcademicDept.ToUpperInvariant();
            }
        }

        // Step 2: Fall back to employee fields
        if (employee == null)
        {
            return null;
        }

        var effortDeptSimple = ToSimpleName(employee.EmpEffortHomeDept);
        if (effortDeptSimple != null && AcademicDepts.Contains(effortDeptSimple))
        {
            return effortDeptSimple.ToUpperInvariant();
        }

        var homeDeptSimple = ToSimpleName(employee.EmpHomeDept);
        if (homeDeptSimple != null && AcademicDepts.Contains(homeDeptSimple))
        {
            return homeDeptSimple.ToUpperInvariant();
        }

        var altDeptSimple = ToSimpleName(employee.EmpAltDeptCode);
        if (altDeptSimple != null && AcademicDepts.Contains(altDeptSimple))
        {
            return altDeptSimple.ToUpperInvariant();
        }

        if (effortDeptSimple != null)
        {
            return effortDeptSimple.ToUpperInvariant();
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

        foreach (var instructor in instructors.Where(i => !string.IsNullOrWhiteSpace(i.EffortTitleCode)))
        {
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

    /// <summary>
    /// Gets a cached lookup of raw department codes to simple names from the dictionary database.
    /// Replicates the logic in dictionary.dbo.fn_get_deptSimpleName function using dvtSVMUnit table.
    /// </summary>
    private async Task<Dictionary<string, string>?> GetDepartmentSimpleNameLookupAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue<Dictionary<string, string>>(DeptSimpleNameCacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var connectionString = _configuration.GetConnectionString("VIPER");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("VIPER connection string not found, cannot load department lookup");
            return null;
        }

        try
        {
            var deptLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            // Query dvtSVMUnit table to get raw code -> simple name mapping
            // This matches the logic in dictionary.dbo.fn_get_deptSimpleName function
            var query = @"
                SELECT DISTINCT
                    dvtSVMUnit_code AS DeptCode,
                    dvtSVMUnit_name_simple AS SimpleName
                FROM [dictionary].[dbo].[dvtSVMUnit]
                WHERE dvtSVMUnit_code IS NOT NULL
                    AND dvtSVMUnit_name_simple IS NOT NULL
                    AND dvtSVMUnit_name_simple <> ''
                    AND dvtSVMUnit_name_simple != 'CCEH'
                    AND ((dvtSvmUnit_Parent_ID IS NULL AND dvtSVMUnit_code = '072000')
                        OR (dvtSvmUnit_Parent_ID = 1 AND dvtSVMUnit_code != '072000')
                        OR (dvtSvmUnit_Parent_ID = 47 AND dvtSVMUnit_code = '072100'))";

            await using var cmd = new SqlCommand(query, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var code = reader["DeptCode"]?.ToString();
                var simpleName = reader["SimpleName"]?.ToString();
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(simpleName))
                {
                    deptLookup[code] = simpleName;
                }
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(DeptSimpleNameCacheKey, deptLookup, cacheOptions);

            _logger.LogInformation("Loaded {Count} department simple names from dictionary database", deptLookup.Count);

            return deptLookup;
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Failed to load department lookup from dictionary database");
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
            EffortType = r.EffortTypeId,
            Role = r.RoleId,
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

    private const string TitleCodesCacheKey = "effort_title_codes_list";
    private const string JobGroupsCacheKey = "effort_job_groups_list";

    public async Task<List<TitleCodeDto>> GetTitleCodesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<List<TitleCodeDto>>(TitleCodesCacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var connectionString = _configuration.GetConnectionString("VIPER");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("VIPER connection string not found, cannot load title codes");
            return [];
        }

        try
        {
            var titleCodes = new List<TitleCodeDto>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            var query = @"
                SELECT DISTINCT
                    dvtTitle_Code AS TitleCode,
                    dvtTitle_name AS TitleName
                FROM [dictionary].[dbo].[dvtTitle]
                WHERE dvtTitle_Code IS NOT NULL
                ORDER BY dvtTitle_name, dvtTitle_Code";

            await using var cmd = new SqlCommand(query, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var code = reader["TitleCode"]?.ToString()?.Trim();
                var name = reader["TitleName"]?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(code))
                {
                    titleCodes.Add(new TitleCodeDto
                    {
                        Code = code,
                        Name = name ?? string.Empty
                    });
                }
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(TitleCodesCacheKey, titleCodes, cacheOptions);

            _logger.LogInformation("Loaded {Count} title codes from dictionary database", titleCodes.Count);

            return titleCodes;
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Failed to load title codes from dictionary database");
            return [];
        }
    }

    public async Task<List<JobGroupDto>> GetJobGroupsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<List<JobGroupDto>>(JobGroupsCacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var connectionString = _configuration.GetConnectionString("VIPER");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("VIPER connection string not found, cannot load job groups");
            return [];
        }

        try
        {
            var jobGroups = new List<JobGroupDto>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            // Get job groups that are actually in use by instructors, joined with dictionary for names
            var query = @"
                SELECT DISTINCT
                    p.JobGroupId AS JobGroupCode,
                    t.dvtTitle_JobGroup_Name AS JobGroupName
                FROM [effort].[Persons] p
                LEFT JOIN [dictionary].[dbo].[dvtTitle] t
                    ON p.JobGroupId = t.dvtTitle_JobGroupID
                WHERE p.JobGroupId IS NOT NULL
                    AND p.JobGroupId != ''
                ORDER BY JobGroupName, JobGroupCode";

            await using var cmd = new SqlCommand(query, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var code = reader["JobGroupCode"]?.ToString()?.Trim();
                var name = reader["JobGroupName"]?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(code))
                {
                    jobGroups.Add(new JobGroupDto
                    {
                        Code = code,
                        // If name is NULL, just use empty string (UI will show code only)
                        Name = name ?? string.Empty
                    });
                }
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(JobGroupsCacheKey, jobGroups, cacheOptions);

            _logger.LogInformation("Loaded {Count} job groups from database", jobGroups.Count);

            return jobGroups;
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Failed to load job groups from database");
            return [];
        }
    }
}
