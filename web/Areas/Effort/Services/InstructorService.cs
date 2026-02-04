using System.Data.Common;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Exceptions;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for instructor-related operations in the Effort system.
/// </summary>
public class InstructorService : IInstructorService
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly AAUDContext _aaudContext;
    private readonly DictionaryContext _dictionaryContext;
    private readonly IEffortAuditService _auditService;
    private readonly ICourseClassificationService _classificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<InstructorService> _logger;
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
        DictionaryContext dictionaryContext,
        IEffortAuditService auditService,
        ICourseClassificationService classificationService,
        IMapper mapper,
        ILogger<InstructorService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _viperContext = viperContext;
        _aaudContext = aaudContext;
        _dictionaryContext = dictionaryContext;
        _auditService = auditService;
        _classificationService = classificationService;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<PersonDto>> GetInstructorsAsync(int termCode, string? department = null, CancellationToken ct = default)
    {
        var baseQuery = _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode);

        if (!string.IsNullOrWhiteSpace(department))
        {
            baseQuery = baseQuery.Where(p => p.EffortDept == department);
        }

        var dtos = await QueryInstructorsWithSenderNamesAsync(baseQuery, ct);

        await Task.WhenAll(
            EnrichWithTitlesAsync(dtos, ct),
            EnrichWithRecordCountsAsync(dtos, termCode, ct));

        // Enrich with percentage assignment summaries
        await EnrichWithPercentageSummariesAsync(dtos, termCode, ct);

        return dtos;
    }

    public async Task<List<PersonDto>> GetInstructorsByDepartmentsAsync(int termCode, IReadOnlyList<string> departments, CancellationToken ct = default)
    {
        if (departments.Count == 0)
        {
            return [];
        }

        var baseQuery = _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode && departments.Contains(p.EffortDept));

        var dtos = await QueryInstructorsWithSenderNamesAsync(baseQuery, ct);

        await Task.WhenAll(
            EnrichWithTitlesAsync(dtos, ct),
            EnrichWithRecordCountsAsync(dtos, termCode, ct));

        // Enrich with percentage assignment summaries
        await EnrichWithPercentageSummariesAsync(dtos, termCode, ct);

        return dtos;
    }

    public async Task<PersonDto?> GetInstructorAsync(int personId, int termCode, CancellationToken ct = default)
    {
        var baseQuery = _context.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId && p.TermCode == termCode);

        var dtos = await QueryInstructorsWithSenderNamesAsync(baseQuery, ct, applyOrdering: false);
        if (dtos.Count == 0) return null;

        var dto = dtos[0];
        await EnrichWithTitlesAsync([dto], ct);
        await EnrichWithRecordCountsAsync([dto], termCode, ct);
        await EnrichWithPercentageSummariesAsync([dto], termCode, ct);
        return dto;
    }

    /// <summary>
    /// Executes instructor query with left joins to resolve LastEmailedBy sender names and MailId.
    /// </summary>
    private async Task<List<PersonDto>> QueryInstructorsWithSenderNamesAsync(
        IQueryable<EffortPerson> baseQuery,
        CancellationToken ct,
        bool applyOrdering = true)
    {
        var query = from p in baseQuery
                    join sender in _context.ViperPersons.AsNoTracking()
                        on p.LastEmailedBy equals sender.PersonId into senders
                    from sender in senders.DefaultIfEmpty()
                    join person in _context.ViperPersons.AsNoTracking()
                        on p.PersonId equals person.PersonId into persons
                    from person in persons.DefaultIfEmpty()
                    select new
                    {
                        Person = p,
                        SenderName = sender != null ? sender.FirstName + " " + sender.LastName : null,
                        MailId = person != null ? person.MailId : null
                    };

        if (applyOrdering)
        {
            query = query.OrderBy(x => x.Person.LastName).ThenBy(x => x.Person.FirstName);
        }

        var results = await query.ToListAsync(ct);
        var instructors = results.Select(r => r.Person).ToList();
        var senderNames = results.ToDictionary(r => r.Person.PersonId, r => r.SenderName);
        var mailIds = results.ToDictionary(r => r.Person.PersonId, r => r.MailId);

        var dtos = _mapper.Map<List<PersonDto>>(instructors);

        foreach (var dto in dtos)
        {
            if (senderNames.TryGetValue(dto.PersonId, out var name) && name != null)
            {
                dto.LastEmailedBy = name;
            }
            if (mailIds.TryGetValue(dto.PersonId, out var mailId))
            {
                dto.MailId = mailId;
            }
        }

        return dtos;
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

        // Match the filtering from the stored procedure:
        // AAUD.person INNER JOIN AAUD.ids ON person_pkey = ids_pkey
        // INNER JOIN AAUD.employees ON emp_pkey = ids_pkey
        // WHERE person_term_code = @TermCode AND ids_mothraID IS NOT NULL
        // NOTE: The SP only filters person by term, not ids or employees
        var mothraIds = people.Select(p => p.MothraId).Where(m => !string.IsNullOrEmpty(m)).ToList();
        var termCodeStr = termCode.ToString();

        if (mothraIds.Count == 0)
        {
            return [];
        }

        // Get AAUD person records for the term - this is the key filter
        // Join to ids to get MothraId mapping (ids is not filtered by term in SP)
        var personWithIds = await _aaudContext.People
            .AsNoTracking()
            .Where(p => p.PersonTermCode == termCodeStr)
            .Join(
                _aaudContext.Ids.Where(i => i.IdsMothraid != null && mothraIds.Contains(i.IdsMothraid)),
                person => person.PersonPKey,
                ids => ids.IdsPKey,
                (person, ids) => new { person.PersonPKey, ids.IdsMothraid })
            .ToListAsync(ct);

        // Build MothraId -> pKey mapping from people who have a person record for the term
        var mothraIdToPKey = personWithIds
            .Where(x => x.IdsMothraid != null)
            .GroupBy(x => x.IdsMothraid!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.PersonPKey).First().PersonPKey, StringComparer.OrdinalIgnoreCase);

        var pKeys = mothraIdToPKey.Values.Distinct().ToList();

        // Get employees (not filtered by term in SP - just joined by pkey)
        var employees = await _aaudContext.Employees
            .AsNoTracking()
            .Where(e => pKeys.Contains(e.EmpPKey))
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

        // Only include people who have all required AAUD records:
        // - AAUD.person record for the term (already filtered in mothraIdToPKey via join)
        // - AAUD.employees record (joined by pKey)
        return people
            .Where(p => mothraIdToPKey.TryGetValue(p.MothraId, out var pKey) &&
                        employeeDict.ContainsKey(pKey))
            .Select(p =>
            {
                var pKey = mothraIdToPKey[p.MothraId];
                var emp = employeeDict[pKey];

                var deptCode = DetermineDepartmentFromJobs(p.MothraId, emp, jobDeptsByMothraId, deptSimpleNameLookup);
                var titleCode = emp.EmpEffortTitleCode?.Trim() ?? emp.EmpTeachingTitleCode?.Trim();

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
            })
            .ToList();
    }

    public async Task<PersonDto> CreateInstructorAsync(CreateInstructorRequest request, CancellationToken ct = default)
    {
        // Check if instructor already exists for this term
        if (await InstructorExistsAsync(request.PersonId, request.TermCode, ct))
        {
            throw new InstructorAlreadyExistsException(request.PersonId, request.TermCode);
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
            instructor.PersonId, LogSanitizer.SanitizeString(instructor.LastName), LogSanitizer.SanitizeString(instructor.FirstName), instructor.TermCode);

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
            personId, LogSanitizer.SanitizeString(instructorInfo.LastName), LogSanitizer.SanitizeString(instructorInfo.FirstName), termCode);

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
        // Check for department override
        var deptOverride = EffortConstants.GetDepartmentOverride(mothraId);
        if (deptOverride != null)
        {
            return deptOverride;
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
        // Check for department override
        var deptOverride = EffortConstants.GetDepartmentOverride(mothraId);
        if (deptOverride != null)
        {
            return deptOverride;
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
    /// Enriches instructor DTOs with effort record counts for the term.
    /// Used for UI display and visual indicators; instructors with zero records
    /// can still receive verification emails to confirm "no effort" status.
    /// Also detects records with 0 hours/weeks which prevent verification.
    /// Generic R-courses (CRN="RESID") are excluded from zero-hour detection.
    /// </summary>
    private async Task EnrichWithRecordCountsAsync(List<PersonDto> instructors, int termCode, CancellationToken ct)
    {
        if (instructors.Count == 0) return;

        var personIds = instructors.Select(i => i.PersonId).ToList();

        var recordStats = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Where(r => r.TermCode == termCode && personIds.Contains(r.PersonId))
            .GroupBy(r => r.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                Count = g.Count(),
                HasZeroHours = g.Any(r => r.Course.Crn != "RESID" && (r.Hours ?? 0) == 0 && (r.Weeks ?? 0) == 0)
            })
            .ToDictionaryAsync(x => x.PersonId, ct);

        foreach (var instructor in instructors)
        {
            if (recordStats.TryGetValue(instructor.PersonId, out var stats))
            {
                instructor.RecordCount = stats.Count;
                instructor.HasZeroHourRecords = stats.HasZeroHours;
            }
            else
            {
                instructor.RecordCount = 0;
                instructor.HasZeroHourRecords = false;
            }
        }
    }

    /// <summary>
    /// Enriches instructor DTOs with percentage assignment summaries grouped by type class.
    /// Queries the Percentages table for assignments overlapping the term's academic year.
    /// </summary>
    private async Task EnrichWithPercentageSummariesAsync(List<PersonDto> instructors, int termCode, CancellationToken ct)
    {
        if (instructors.Count == 0) return;

        var personIds = instructors.Select(i => i.PersonId).ToList();

        // Get the academic year from vwTerms for the given term code
        var term = await _viperContext.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermCode == termCode, ct);

        if (term == null)
        {
            _logger.LogWarning("Term {TermCode} not found in vwTerms", termCode);
            return;
        }

        var range = AcademicYearHelper.GetDateRange(term.AcademicYear);

        // Get all percentage assignments for these persons that overlap with the academic year
        var allPercentages = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .Where(p => personIds.Contains(p.PersonId))
            .Where(p => p.StartDate < range.EndDateExclusive)
            .Where(p => p.EndDate == null || p.EndDate >= range.StartDate)
            .ToListAsync(ct);

        // Apply display filtering:
        // - Clinical types: only show if percent > 0.01 (> 1%)
        // - Non-Clinical types: only show if percent > 0 OR type_name != "None"
        var percentages = allPercentages
            .Where(p =>
            {
                var isClinical = string.Equals(p.PercentAssignType.Class, "Clinical", StringComparison.OrdinalIgnoreCase);
                if (isClinical)
                {
                    return p.PercentageValue > 0.01;
                }
                return p.PercentageValue > 0 || p.PercentAssignType.Name != "None";
            })
            .ToList();

        // Group by person and type class
        var grouped = percentages
            .GroupBy(p => p.PersonId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(p => p.PercentAssignType.Class?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        c => c.Key,
                        c => c.ToList(),
                        StringComparer.OrdinalIgnoreCase
                    )
            );

        foreach (var instructor in instructors)
        {
            if (!grouped.TryGetValue(instructor.PersonId, out var byClass))
            {
                continue;
            }

            // Format Admin summary
            if (byClass.TryGetValue("Admin", out var adminAssignments))
            {
                instructor.PercentAdminSummary = FormatPercentageSummary(adminAssignments);
            }

            // Format Clinical summary
            if (byClass.TryGetValue("Clinical", out var clinicalAssignments))
            {
                instructor.PercentClinicalSummary = FormatPercentageSummary(clinicalAssignments);
            }

            // Format Other summary (any class that isn't Admin or Clinical)
            var otherAssignments = byClass
                .Where(kvp => !string.Equals(kvp.Key, "Admin", StringComparison.OrdinalIgnoreCase) &&
                              !string.Equals(kvp.Key, "Clinical", StringComparison.OrdinalIgnoreCase))
                .SelectMany(kvp => kvp.Value)
                .ToList();
            if (otherAssignments.Count > 0)
            {
                instructor.PercentOtherSummary = FormatPercentageSummary(otherAssignments);
            }
        }
    }

    /// <summary>
    /// Formats a list of percentage assignments into a summary string for display.
    /// Format:
    ///   {percent}% - {modifier} {type_name}
    ///   {unit_name} ({comment})
    ///   {startDate} - {endDate or INDEF}
    /// </summary>
    private static string FormatPercentageSummary(List<Percentage> assignments)
    {
        var summaries = new List<string>();

        foreach (var a in assignments.OrderByDescending(p => p.PercentageValue))
        {
            // Convert stored decimal (0-1) to percentage (0-100)
            var percent = Math.Round(a.PercentageValue * 100, 0);
            var lines = new List<string>();

            // Line 1: Percent and type display
            // Only show modifier + type_name if type_name is NOT "None" AND NOT "Clinical"
            var typeName = a.PercentAssignType.Name;
            string line1;
            if (typeName != "None" && !string.Equals(typeName, "Clinical", StringComparison.OrdinalIgnoreCase))
            {
                var typeDisplay = string.IsNullOrWhiteSpace(a.Modifier)
                    ? typeName
                    : $"{a.Modifier.Trim()} {typeName}";
                line1 = $"{percent}% - {typeDisplay}";
            }
            else
            {
                line1 = $"{percent}%";
            }
            lines.Add(line1);

            // Line 2: Unit and comment (legacy shows unit_name then (comment) in parentheses)
            var line2Parts = new List<string>();
            if (a.Unit != null)
            {
                line2Parts.Add(a.Unit.Name);
            }
            if (!string.IsNullOrWhiteSpace(a.Comment))
            {
                line2Parts.Add($"({a.Comment.Trim()})");
            }
            if (line2Parts.Count > 0)
            {
                lines.Add(string.Join(" ", line2Parts));
            }

            // Line 3: Date range (actual dates, not clamped to academic year)
            // Legacy uses "INDEF" for null end date
            var startStr = a.StartDate.ToString("MMM yyyy");
            var endStr = a.EndDate.HasValue ? a.EndDate.Value.ToString("MMM yyyy") : "INDEF";
            lines.Add($"{startStr} - {endStr}");

            summaries.Add(string.Join("\n", lines));
        }

        // Join multiple assignments with blank line between them
        return string.Join("\n\n", summaries);
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

        try
        {
            var titles = await _dictionaryContext.Titles
                .AsNoTracking()
                .Where(t => t.Code != null && t.Abbreviation != null)
                .Select(t => new { t.Code, t.Abbreviation })
                .Distinct()
                .ToListAsync(ct);

            var titleLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var title in titles.Where(t =>
                !string.IsNullOrEmpty(t.Code) && !string.IsNullOrEmpty(t.Abbreviation)))
            {
                titleLookup[title.Code!] = title.Abbreviation!;
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(TitleCacheKey, titleLookup, cacheOptions);

            _logger.LogInformation("Loaded {Count} title codes from dictionary database", titleLookup.Count);

            return titleLookup;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbException)
        {
            _logger.LogWarning(ex, "Failed to load title lookup from dictionary database");
            return null;
        }
    }

    /// <summary>
    /// Gets a cached lookup of raw department codes to simple names from the dictionary database.
    /// Replicates the logic in dictionary.dbo.fn_get_deptSimpleName function using dvtSVMUnit table.
    /// </summary>
    public async Task<Dictionary<string, string>?> GetDepartmentSimpleNameLookupAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<Dictionary<string, string>>(DeptSimpleNameCacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        try
        {
            // Query dvtSVMUnit table to get raw code -> simple name mapping
            // This matches the logic in dictionary.dbo.fn_get_deptSimpleName function
            var units = await _dictionaryContext.SvmUnits
                .AsNoTracking()
                .Where(u => u.Code != null &&
                            u.SimpleName != null &&
                            u.SimpleName != "" &&
                            u.SimpleName != "CCEH" &&
                            ((u.ParentId == null && u.Code == "072000") ||
                             (u.ParentId == 1 && u.Code != "072000") ||
                             (u.ParentId == 47 && u.Code == "072100")))
                .Select(u => new { u.Code, u.SimpleName })
                .Distinct()
                .ToListAsync(ct);

            var deptLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var unit in units.Where(u =>
                !string.IsNullOrEmpty(u.Code) && !string.IsNullOrEmpty(u.SimpleName)))
            {
                deptLookup[unit.Code!] = unit.SimpleName!;
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(DeptSimpleNameCacheKey, deptLookup, cacheOptions);

            _logger.LogInformation("Loaded {Count} department simple names from dictionary database", deptLookup.Count);

            return deptLookup;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbException)
        {
            _logger.LogWarning(ex, "Failed to load department lookup from dictionary database");
            return null;
        }
    }

    public async Task<List<InstructorEffortRecordDto>> GetInstructorEffortRecordsAsync(
        int personId, int termCode, CancellationToken ct = default)
    {
        // Get the effort records with course and role info
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

        // Get the unique course IDs from the records
        var courseIds = records.Select(r => r.CourseId).Distinct().ToList();

        // Fetch child relationships separately to avoid cycle issue with AsNoTracking
        var childRelationships = await _context.CourseRelationships
            .AsNoTracking()
            .Include(cr => cr.ChildCourse)
            .Where(cr => courseIds.Contains(cr.ParentCourseId))
            .ToListAsync(ct);

        // Group child relationships by parent course ID
        var childrenByParent = childRelationships
            .GroupBy(cr => cr.ParentCourseId)
            .ToDictionary(g => g.Key, g => g.ToList());

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
                CustDept = r.Course.CustDept,
                IsDvm = _classificationService.IsDvmCourse(r.Course.SubjCode),
                Is199299 = _classificationService.Is199299Course(r.Course.CrseNumb),
                IsRCourse = _classificationService.IsRCourse(r.Course.CrseNumb)
            },
            ChildCourses = childrenByParent.TryGetValue(r.CourseId, out var children)
                ? children
                    .Where(cr => cr.ChildCourse != null)
                    .Select(cr => new ChildCourseDto
                    {
                        Id = cr.ChildCourse!.Id,
                        SubjCode = cr.ChildCourse.SubjCode,
                        CrseNumb = cr.ChildCourse.CrseNumb,
                        SeqNumb = cr.ChildCourse.SeqNumb,
                        Units = cr.ChildCourse.Units,
                        Enrollment = cr.ChildCourse.Enrollment,
                        RelationshipType = cr.RelationshipType
                    }).ToList()
                : new List<ChildCourseDto>()
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

        try
        {
            var titles = await _dictionaryContext.Titles
                .AsNoTracking()
                .Where(t => t.Code != null)
                .Select(t => new { Code = t.Code!.Trim(), Name = t.Name ?? "" })
                .Distinct()
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Code)
                .ToListAsync(ct);

            var titleCodes = titles
                .Where(t => !string.IsNullOrEmpty(t.Code))
                .Select(t => new TitleCodeDto
                {
                    Code = t.Code,
                    Name = t.Name.Trim()
                })
                .ToList();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(TitleCodesCacheKey, titleCodes, cacheOptions);

            _logger.LogInformation("Loaded {Count} title codes from dictionary database", titleCodes.Count);

            return titleCodes;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbException)
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

        try
        {
            // Get job groups that are actually in use by instructors
            var usedJobGroupIds = await _context.Persons
                .AsNoTracking()
                .Where(p => p.JobGroupId != null && p.JobGroupId != "")
                .Select(p => p.JobGroupId!)
                .Distinct()
                .ToListAsync(ct);

            if (usedJobGroupIds.Count == 0)
            {
                return [];
            }

            // Get job group names from dictionary
            var titleJobGroups = await _dictionaryContext.Titles
                .AsNoTracking()
                .Where(t => t.JobGroupId != null && usedJobGroupIds.Contains(t.JobGroupId))
                .Select(t => new { t.JobGroupId, t.JobGroupName })
                .Distinct()
                .ToListAsync(ct);

            var jobGroupNameLookup = titleJobGroups
                .Where(t => !string.IsNullOrEmpty(t.JobGroupId))
                .GroupBy(t => t.JobGroupId!)
                .ToDictionary(g => g.Key, g => g.First().JobGroupName ?? "", StringComparer.OrdinalIgnoreCase);

            var jobGroups = usedJobGroupIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => new JobGroupDto
                {
                    Code = id.Trim(),
                    Name = jobGroupNameLookup.TryGetValue(id.Trim(), out var name) ? name.Trim() : ""
                })
                .OrderBy(j => j.Name)
                .ThenBy(j => j.Code)
                .ToList();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _cache.Set(JobGroupsCacheKey, jobGroups, cacheOptions);

            _logger.LogInformation("Loaded {Count} job groups from database", jobGroups.Count);

            return jobGroups;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbException)
        {
            _logger.LogWarning(ex, "Failed to load job groups from database");
            return [];
        }
    }
}
