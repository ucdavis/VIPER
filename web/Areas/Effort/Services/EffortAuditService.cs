using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for logging and querying audit trail entries in the Effort system.
/// </summary>
public class EffortAuditService : IEffortAuditService
{
    /// <summary>
    /// Maximum length for search text.
    /// </summary>
    private const int MaxSearchTextLength = 20;

    private readonly EffortDbContext _context;
    private readonly AAUDContext _aaudContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<EffortAuditService> _logger;

    public EffortAuditService(
        EffortDbContext context,
        AAUDContext aaudContext,
        IHttpContextAccessor httpContextAccessor,
        IUserHelper userHelper,
        ILogger<EffortAuditService> logger)
    {
        _context = context;
        _aaudContext = aaudContext;
        _httpContextAccessor = httpContextAccessor;
        _userHelper = userHelper;
        _logger = logger;
    }

    // ==================== Logging Methods ====================

    public async Task LogPercentageChangeAsync(int percentageId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default)
    {
        await CreateAuditEntryAsync(EffortAuditTables.Percentages, percentageId, termCode, action,
            SerializeChanges(oldValues, newValues), ct);
    }

    public async Task LogRecordChangeAsync(int recordId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default)
    {
        await CreateAuditEntryAsync(EffortAuditTables.Records, recordId, termCode, action,
            SerializeChanges(oldValues, newValues), ct);
    }

    public async Task LogPersonChangeAsync(int personId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default)
    {
        await CreateAuditEntryAsync(EffortAuditTables.Persons, personId, termCode, action,
            SerializeChanges(oldValues, newValues), ct);
    }

    public async Task LogCourseChangeAsync(int courseId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default)
    {
        await CreateAuditEntryAsync(EffortAuditTables.Courses, courseId, termCode, action,
            SerializeChanges(oldValues, newValues), ct);
    }

    public async Task LogTermChangeAsync(int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default)
    {
        await CreateAuditEntryAsync(EffortAuditTables.Terms, termCode, termCode, action,
            SerializeChanges(oldValues, newValues), ct);
    }

    public async Task LogImportAsync(int termCode, string action, string details, CancellationToken ct = default)
    {
        await CreateAuditEntryAsync(EffortAuditTables.Terms, termCode, termCode, action, details, ct);
    }

    public void AddTermChangeAudit(int termCode, string action, object? oldValues, object? newValues)
    {
        AddAuditEntry(EffortAuditTables.Terms, termCode, termCode, action, SerializeChanges(oldValues, newValues));
    }

    public void AddImportAudit(int termCode, string action, string details)
    {
        AddAuditEntry(EffortAuditTables.Terms, termCode, termCode, action, details);
    }

    /// <summary>
    /// Add an audit entry to the context without saving.
    /// Use within a transaction where the caller manages SaveChangesAsync.
    /// </summary>
    private void AddAuditEntry(string tableName, int recordId, int termCode, string action, string? changes)
    {
        var (ipAddress, userAgent) = GetRequestInfo();
        var changedBy = GetCurrentPersonId();

        var audit = new Audit
        {
            TableName = tableName,
            RecordId = recordId,
            TermCode = termCode,
            Action = action,
            ChangedBy = changedBy,
            ChangedDate = DateTime.Now,
            Changes = changes,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent
        };

        _context.Audits.Add(audit);

        _logger.LogDebug("Added audit entry to context: {Action} on {TableName} record {RecordId} by user {ChangedBy}",
            action, tableName, recordId, LogSanitizer.SanitizeId(changedBy.ToString()));
    }

    /// <summary>
    /// Create an audit entry with common properties.
    /// </summary>
    private async Task CreateAuditEntryAsync(
        string tableName,
        int recordId,
        int termCode,
        string action,
        string? changes,
        CancellationToken ct)
    {
        try
        {
            var (ipAddress, userAgent) = GetRequestInfo();
            var changedBy = GetCurrentPersonId();

            var audit = new Audit
            {
                TableName = tableName,
                RecordId = recordId,
                TermCode = termCode,
                Action = action,
                ChangedBy = changedBy,
                ChangedDate = DateTime.Now,
                Changes = changes,
                IpAddress = ipAddress,
                UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Created audit entry: {Action} on {TableName} record {RecordId} by user {ChangedBy}",
                action, tableName, recordId, LogSanitizer.SanitizeId(changedBy.ToString()));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating audit entry for {Action} on {TableName} record {RecordId}",
                action, tableName, recordId);
            throw new InvalidOperationException($"Failed to create audit entry for '{action}'. Please try again.", ex);
        }
    }

    // ==================== Query Methods ====================

    /// <inheritdoc />
    public async Task<List<EffortAuditRow>> GetAuditEntriesAsync(EffortAuditFilter filter,
        int page, int perPage, string? sortBy, bool descending, CancellationToken ct = default)
    {
        // Defensive bounds checks for pagination
        page = Math.Max(1, page);
        perPage = Math.Clamp(perPage, 1, 500);

        var query = BuildFilteredQuery(filter);
        query = ApplySorting(query, sortBy, descending);

        var skip = (page - 1) * perPage;
        var audits = await query
            .Skip(skip)
            .Take(perPage)
            .ToListAsync(ct);

        var changedByIds = audits.Select(a => a.ChangedBy).Distinct().ToList();
        var userNames = await _aaudContext.AaudUsers
            .AsNoTracking()
            .Where(u => changedByIds.Contains(u.AaudUserId))
            .Select(u => new { u.AaudUserId, u.DisplayLastName, u.DisplayFirstName })
            .ToDictionaryAsync(u => u.AaudUserId, u => $"{u.DisplayLastName}, {u.DisplayFirstName}", ct);

        var results = audits.Select(audit =>
        {
            var row = new EffortAuditRow
            {
                Id = audit.Id,
                TableName = audit.TableName,
                RecordId = audit.RecordId,
                Action = audit.Action,
                ChangedDate = audit.ChangedDate,
                ChangedBy = audit.ChangedBy,
                ChangedByName = userNames.GetValueOrDefault(audit.ChangedBy, $"User {audit.ChangedBy}"),
                Changes = audit.Changes
            };

            if (!string.IsNullOrEmpty(audit.Changes))
            {
                try
                {
                    row.ChangesDetail = JsonSerializer.Deserialize<Dictionary<string, ChangeDetail>>(audit.Changes);
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(ex, "Unable to parse audit Changes as JSON for auditId={AuditId}", audit.Id);
                    // Legacy audit entries may have non-JSON text; ChangesDetail stays null
                }
            }

            return (row, audit);
        }).ToList();

        await BatchEnrichAuditRowsAsync(results, ct);

        return results.Select(r => r.row).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetAuditEntryCountAsync(EffortAuditFilter filter, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(filter);
        return await query.CountAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctActionsAsync(bool excludeImports, CancellationToken ct = default)
    {
        var query = _context.Audits
            .AsNoTracking()
            .Select(a => a.Action)
            .Distinct();

        if (excludeImports)
        {
            query = query.Where(a => !EffortAuditActions.ImportActions.Contains(a));
        }

        return await query.OrderBy(a => a).ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ModifierInfo>> GetDistinctModifiersAsync(bool excludeImports = false, CancellationToken ct = default)
    {
        var query = _context.Audits.AsNoTracking();

        if (excludeImports)
        {
            query = query.Where(a => !EffortAuditActions.ImportActions.Contains(a.Action));
        }

        var modifierIds = await query
            .Select(a => a.ChangedBy)
            .Distinct()
            .ToListAsync(ct);

        var modifiers = await _aaudContext.AaudUsers
            .AsNoTracking()
            .Where(u => modifierIds.Contains(u.AaudUserId))
            .Select(u => new ModifierInfo
            {
                PersonId = u.AaudUserId,
                FullName = u.DisplayLastName + ", " + u.DisplayFirstName
            })
            .OrderBy(m => m.FullName)
            .ToListAsync(ct);

        return modifiers;
    }

    /// <inheritdoc />
    public async Task<List<int>> GetAuditTermCodesAsync(bool excludeImports = false, CancellationToken ct = default)
    {
        // Base query with optional import filtering
        var baseQuery = _context.Audits.AsNoTracking();
        if (excludeImports)
        {
            baseQuery = baseQuery.Where(a => !EffortAuditActions.ImportActions.Contains(a.Action));
        }

        // Get term codes from TermCode column and from Records/Persons joins
        var legacyTerms = await baseQuery
            .Where(a => a.TermCode != null)
            .Select(a => a.TermCode!.Value)
            .Distinct()
            .ToListAsync(ct);

        // Also check Records and Persons tables for RecordId matches
        var recordTerms = await baseQuery
            .Where(a => a.TableName == EffortAuditTables.Records)
            .Join(_context.Records, a => a.RecordId, r => r.Id, (a, r) => r.TermCode)
            .Distinct()
            .ToListAsync(ct);

        var personTerms = await baseQuery
            .Where(a => a.TableName == EffortAuditTables.Persons)
            .Join(_context.Persons, a => a.RecordId, p => p.PersonId, (a, p) => p.TermCode)
            .Distinct()
            .ToListAsync(ct);

        var termTerms = await baseQuery
            .Where(a => a.TableName == EffortAuditTables.Terms)
            .Select(a => a.RecordId)
            .Distinct()
            .ToListAsync(ct);

        return legacyTerms
            .Union(recordTerms)
            .Union(personTerms)
            .Union(termTerms)
            .Distinct()
            .OrderByDescending(t => t)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<ModifierInfo>> GetDistinctInstructorsAsync(bool excludeImports = false, CancellationToken ct = default)
    {
        // Base query with optional import filtering
        var baseQuery = _context.Audits.AsNoTracking();
        if (excludeImports)
        {
            baseQuery = baseQuery.Where(a => !EffortAuditActions.ImportActions.Contains(a.Action));
        }

        // Get PersonIds from Records table audits
        var recordPersonIds = baseQuery
            .Where(a => a.TableName == EffortAuditTables.Records)
            .Join(_context.Records, a => a.RecordId, r => r.Id, (a, r) => r.PersonId)
            .Distinct();

        // Get PersonIds from Persons table audits (RecordId IS the PersonId)
        var personPersonIds = baseQuery
            .Where(a => a.TableName == EffortAuditTables.Persons)
            .Select(a => a.RecordId)
            .Distinct();

        // Get PersonIds from Percentages table audits
        var percentagePersonIds = baseQuery
            .Where(a => a.TableName == EffortAuditTables.Percentages)
            .Join(_context.Percentages, a => a.RecordId, p => p.Id, (a, p) => p.PersonId)
            .Distinct();

        // Combine all PersonIds
        var allPersonIds = await recordPersonIds
            .Union(personPersonIds)
            .Union(percentagePersonIds)
            .Distinct()
            .ToListAsync(ct);

        // Get all person records for the matching PersonIds, then group in memory
        var personRecords = await _context.Persons
            .AsNoTracking()
            .Where(p => allPersonIds.Contains(p.PersonId))
            .Select(p => new { p.PersonId, p.LastName, p.FirstName, p.TermCode })
            .ToListAsync(ct);

        // Group by PersonId and select most recent term's name (in memory)
        var instructors = personRecords
            .GroupBy(p => p.PersonId)
            .Select(g =>
            {
                var mostRecent = g.OrderByDescending(p => p.TermCode).First();
                return new ModifierInfo
                {
                    PersonId = mostRecent.PersonId,
                    FullName = mostRecent.LastName + ", " + mostRecent.FirstName
                };
            })
            .OrderBy(i => i.FullName)
            .ToList();

        return instructors;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctSubjectCodesAsync(int? termCode = null, string? courseNumber = null, bool excludeImports = false, CancellationToken ct = default)
    {
        var query = BuildAuditCourseQuery(termCode, excludeImports);

        if (!string.IsNullOrEmpty(courseNumber))
        {
            var normalizedCourseNumber = courseNumber.Trim().ToUpper();
            query = query.Where(c => c.CrseNumb.Trim().ToUpper() == normalizedCourseNumber);
        }

        return await query
            .Select(c => c.SubjCode.Trim().ToUpper())
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctCourseNumbersAsync(int? termCode = null, string? subjectCode = null, bool excludeImports = false, CancellationToken ct = default)
    {
        var query = BuildAuditCourseQuery(termCode, excludeImports);

        if (!string.IsNullOrEmpty(subjectCode))
        {
            var normalizedSubjectCode = subjectCode.Trim().ToUpper();
            query = query.Where(c => c.SubjCode.Trim().ToUpper() == normalizedSubjectCode);
        }

        return await query
            .Select(c => c.CrseNumb.Trim().ToUpper())
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);
    }

    private IQueryable<EffortCourse> BuildAuditCourseQuery(int? termCode, bool excludeImports = false)
    {
        var auditQuery = _context.Audits
            .AsNoTracking()
            .Where(a => a.TableName == EffortAuditTables.Records);

        if (excludeImports)
        {
            auditQuery = auditQuery.Where(a => !EffortAuditActions.ImportActions.Contains(a.Action));
        }

        var recordAuditQuery = auditQuery.Join(_context.Records, a => a.RecordId, r => r.Id, (a, r) => r);

        if (termCode.HasValue)
        {
            recordAuditQuery = recordAuditQuery.Where(r => r.TermCode == termCode.Value);
        }

        var recordAuditCourseIds = recordAuditQuery.Select(r => r.CourseId).Distinct();

        return _context.Courses
            .AsNoTracking()
            .Where(c => recordAuditCourseIds.Contains(c.Id));
    }

    // ==================== Helper Methods ====================

    private IQueryable<Audit> BuildFilteredQuery(EffortAuditFilter filter)
    {
        var query = _context.Audits.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(a => a.ChangedDate >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            var endOfDay = filter.DateTo.Value.AddDays(1);
            query = query.Where(a => a.ChangedDate < endOfDay);
        }

        if (filter.ModifiedByPersonId.HasValue)
        {
            query = query.Where(a => a.ChangedBy == filter.ModifiedByPersonId.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            var search = filter.SearchText.Trim();
            if (search.Length > MaxSearchTextLength)
            {
                search = search[..MaxSearchTextLength];
            }
            query = query.Where(a => a.Changes != null && a.Changes.Contains(search));
        }

        if (filter.ExcludeImports)
        {
            query = query.Where(a => !EffortAuditActions.ImportActions.Contains(a.Action));
        }

        if (filter.TermCode.HasValue)
        {
            var termCode = filter.TermCode.Value;
            query = query.Where(a =>
                a.TermCode == termCode ||
                (a.TableName == EffortAuditTables.Terms && a.RecordId == termCode));
        }

        if (!string.IsNullOrEmpty(filter.SubjectCode))
        {
            var subjectCode = filter.SubjectCode.Trim().ToUpper();
            var matchingCourseIds = _context.Courses
                .Where(c => c.SubjCode.Trim().ToUpper() == subjectCode)
                .Select(c => c.Id);
            var matchingRecordIds = _context.Records
                .Where(r => matchingCourseIds.Contains(r.CourseId))
                .Select(r => r.Id);

            query = query.Where(a =>
                (a.TableName == EffortAuditTables.Courses && matchingCourseIds.Contains(a.RecordId)) ||
                (a.TableName == EffortAuditTables.Records && matchingRecordIds.Contains(a.RecordId)));
        }

        if (!string.IsNullOrEmpty(filter.CourseNumber))
        {
            var courseNumber = filter.CourseNumber.Trim().ToUpper();
            var matchingCourseIds = _context.Courses
                .Where(c => c.CrseNumb.Trim().ToUpper() == courseNumber)
                .Select(c => c.Id);
            var matchingRecordIds = _context.Records
                .Where(r => matchingCourseIds.Contains(r.CourseId))
                .Select(r => r.Id);

            query = query.Where(a =>
                (a.TableName == EffortAuditTables.Courses && matchingCourseIds.Contains(a.RecordId)) ||
                (a.TableName == EffortAuditTables.Records && matchingRecordIds.Contains(a.RecordId)));
        }

        if (filter.InstructorPersonId.HasValue)
        {
            var instructorId = filter.InstructorPersonId.Value;

            // Records where PersonId matches
            var matchingRecordIds = _context.Records
                .Where(r => r.PersonId == instructorId)
                .Select(r => r.Id);

            // Percentages where PersonId matches
            var matchingPercentageIds = _context.Percentages
                .Where(p => p.PersonId == instructorId)
                .Select(p => p.Id);

            query = query.Where(a =>
                (a.TableName == EffortAuditTables.Records && matchingRecordIds.Contains(a.RecordId)) ||
                (a.TableName == EffortAuditTables.Persons && a.RecordId == instructorId) ||
                (a.TableName == EffortAuditTables.Percentages && matchingPercentageIds.Contains(a.RecordId)));
        }

        return query;
    }

    private IQueryable<Audit> ApplySorting(IQueryable<Audit> query, string? sortBy, bool descending)
    {
        switch (sortBy?.ToLowerInvariant())
        {
            case "action":
                return descending
                    ? query.OrderByDescending(a => a.Action)
                    : query.OrderBy(a => a.Action);

            case "tablename":
                return descending
                    ? query.OrderByDescending(a => a.TableName)
                    : query.OrderBy(a => a.TableName);

            case "changedbyname":
            case "changedby":
                return descending
                    ? query.OrderByDescending(a => a.ChangedBy)
                    : query.OrderBy(a => a.ChangedBy);

            case "instructorname":
                return ApplyInstructorNameSorting(query, descending);

            case "termname":
                return ApplyTermNameSorting(query, descending);

            case "coursecode":
                return ApplyCourseCodeSorting(query, descending);

            default: // changeddate
                return descending
                    ? query.OrderByDescending(a => a.ChangedDate)
                    : query.OrderBy(a => a.ChangedDate);
        }
    }

    private IQueryable<Audit> ApplyInstructorNameSorting(IQueryable<Audit> query, bool descending)
    {
        // Use left join for Records table, subquery for Persons table (PersonId not unique)
        var joinedQuery = from a in query
                          join r in _context.Records.Include(r => r.Person)
                              on new { a.TableName, a.RecordId }
                              equals new { TableName = EffortAuditTables.Records, RecordId = r.Id }
                              into records
                          from r in records.DefaultIfEmpty()
                          let personLastName = a.TableName == EffortAuditTables.Persons
                              ? _context.Persons
                                  .Where(p => p.PersonId == a.RecordId)
                                  .OrderByDescending(p => p.TermCode)
                                  .Select(p => p.LastName)
                                  .FirstOrDefault() ?? ""
                              : ""
                          select new
                          {
                              Audit = a,
                              SortKey = r != null && r.Person != null
                                  ? r.Person.LastName
                                  : personLastName
                          };

        return descending
            ? joinedQuery.OrderByDescending(x => x.SortKey).Select(x => x.Audit)
            : joinedQuery.OrderBy(x => x.SortKey).Select(x => x.Audit);
    }

    private IQueryable<Audit> ApplyTermNameSorting(IQueryable<Audit> query, bool descending)
    {
        // Use left join for Records table, subquery for Persons table
        var joinedQuery = from a in query
                          join r in _context.Records
                              on new { a.TableName, a.RecordId }
                              equals new { TableName = EffortAuditTables.Records, RecordId = r.Id }
                              into records
                          from r in records.DefaultIfEmpty()
                          select new
                          {
                              Audit = a,
                              SortKey = a.TermCode
                                  ?? (r != null ? (int?)r.TermCode : null)
                                  ?? (a.TableName == EffortAuditTables.Terms ? (int?)a.RecordId : null)
                                  ?? (a.TableName == EffortAuditTables.Persons
                                      ? _context.Persons
                                          .Where(p => p.PersonId == a.RecordId)
                                          .OrderByDescending(p => p.TermCode)
                                          .Select(p => (int?)p.TermCode)
                                          .FirstOrDefault()
                                      : null)
                          };

        return descending
            ? joinedQuery.OrderByDescending(x => x.SortKey).Select(x => x.Audit)
            : joinedQuery.OrderBy(x => x.SortKey).Select(x => x.Audit);
    }

    private IQueryable<Audit> ApplyCourseCodeSorting(IQueryable<Audit> query, bool descending)
    {
        // Use left joins for Records and Courses tables (both have unique Id)
        var joinedQuery = from a in query
                          join r in _context.Records.Include(r => r.Course)
                              on new { a.TableName, a.RecordId }
                              equals new { TableName = EffortAuditTables.Records, RecordId = r.Id }
                              into records
                          from r in records.DefaultIfEmpty()
                          join c in _context.Courses
                              on new { a.TableName, CourseId = a.RecordId }
                              equals new { TableName = EffortAuditTables.Courses, CourseId = c.Id }
                              into courses
                          from c in courses.DefaultIfEmpty()
                          let courseCodeFromCourse = c != null
                              ? c.SubjCode.Trim() + " " + c.CrseNumb.Trim()
                              : ""
                          select new
                          {
                              Audit = a,
                              SortKey = r != null && r.Course != null
                                  ? r.Course.SubjCode.Trim() + " " + r.Course.CrseNumb.Trim()
                                  : courseCodeFromCourse
                          };

        return descending
            ? joinedQuery.OrderByDescending(x => x.SortKey).Select(x => x.Audit)
            : joinedQuery.OrderBy(x => x.SortKey).Select(x => x.Audit);
    }

    /// <summary>
    /// Batch enrich audit rows to avoid N+1 queries.
    /// Groups audits by TableName and loads related entities in batches.
    /// </summary>
    private async Task BatchEnrichAuditRowsAsync(
        List<(EffortAuditRow row, Audit audit)> items,
        CancellationToken ct)
    {
        if (items.Count == 0) return;

        foreach (var (row, audit) in items)
        {
            if (audit.TermCode.HasValue)
            {
                row.TermCode = audit.TermCode;
                row.TermName = GetTermName(audit.TermCode.Value);
            }
        }

        var groupedByTable = items.GroupBy(i => i.audit.TableName).ToList();

        foreach (var group in groupedByTable)
        {
            var tableName = group.Key;
            var groupItems = group.ToList();
            var recordIds = groupItems.Select(i => i.audit.RecordId).Distinct().ToList();

            switch (tableName)
            {
                case EffortAuditTables.Terms:
                    EnrichTermRows(groupItems);
                    break;
                case EffortAuditTables.Records:
                    await EnrichRecordRowsBatchAsync(groupItems, recordIds, ct);
                    break;
                case EffortAuditTables.Persons:
                    await EnrichPersonRowsBatchAsync(groupItems, recordIds, ct);
                    break;
                case EffortAuditTables.Courses:
                    await EnrichCourseRowsBatchAsync(groupItems, recordIds, ct);
                    break;
                case EffortAuditTables.Percentages:
                    await EnrichPercentageRowsBatchAsync(groupItems, recordIds, ct);
                    break;
            }
        }
    }

    private static void EnrichTermRows(List<(EffortAuditRow row, Audit audit)> items)
    {
        foreach (var (row, audit) in items)
        {
            row.TermCode = audit.RecordId;
            row.TermName = GetTermName(audit.RecordId);
        }
    }

    private async Task EnrichRecordRowsBatchAsync(
        List<(EffortAuditRow row, Audit audit)> items,
        List<int> recordIds,
        CancellationToken ct)
    {
        var records = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Include(r => r.Person)
            .Where(r => recordIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, ct);

        foreach (var (row, audit) in items)
        {
            if (records.TryGetValue(audit.RecordId, out var record))
            {
                row.TermCode = record.TermCode;
                row.TermName = GetTermName(record.TermCode);
                row.InstructorPersonId = record.PersonId;
                row.InstructorName = $"{record.Person?.LastName}, {record.Person?.FirstName}";
                row.Crn = record.Crn;
                if (record.Course != null)
                {
                    row.CourseCode = $"{record.Course.SubjCode?.Trim()} {record.Course.CrseNumb?.Trim()}-{record.Course.SeqNumb?.Trim()}";
                }
            }
        }
    }

    private async Task EnrichPersonRowsBatchAsync(
        List<(EffortAuditRow row, Audit audit)> items,
        List<int> personIds,
        CancellationToken ct)
    {
        var personRecords = await _context.Persons
            .AsNoTracking()
            .Where(p => personIds.Contains(p.PersonId))
            .ToListAsync(ct);

        var mostRecentPersons = personRecords
            .GroupBy(p => p.PersonId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.TermCode).First());

        foreach (var (row, audit) in items)
        {
            if (mostRecentPersons.TryGetValue(audit.RecordId, out var person))
            {
                row.TermCode = person.TermCode;
                row.TermName = GetTermName(person.TermCode);
                row.InstructorPersonId = person.PersonId;
                row.InstructorName = $"{person.LastName}, {person.FirstName}";
            }
        }
    }

    private async Task EnrichCourseRowsBatchAsync(
        List<(EffortAuditRow row, Audit audit)> items,
        List<int> courseIds,
        CancellationToken ct)
    {
        var courses = await _context.Courses
            .AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        foreach (var (row, audit) in items)
        {
            if (courses.TryGetValue(audit.RecordId, out var course))
            {
                row.TermCode = course.TermCode;
                row.TermName = GetTermName(course.TermCode);
                row.Crn = course.Crn;
                row.CourseCode = $"{course.SubjCode?.Trim()} {course.CrseNumb?.Trim()}-{course.SeqNumb?.Trim()}";
            }
        }
    }

    private async Task EnrichPercentageRowsBatchAsync(
        List<(EffortAuditRow row, Audit audit)> items,
        List<int> percentageIds,
        CancellationToken ct)
    {
        var percentages = await _context.Percentages
            .AsNoTracking()
            .Where(p => percentageIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var personIds = percentages.Values.Select(p => p.PersonId).Distinct().ToList();

        var personRecords = await _context.Persons
            .AsNoTracking()
            .Where(p => personIds.Contains(p.PersonId))
            .ToListAsync(ct);

        var mostRecentPersons = personRecords
            .GroupBy(p => p.PersonId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.TermCode).First());

        foreach (var (row, audit) in items)
        {
            if (percentages.TryGetValue(audit.RecordId, out var percentage))
            {
                row.InstructorPersonId = percentage.PersonId;
                if (mostRecentPersons.TryGetValue(percentage.PersonId, out var person))
                {
                    row.InstructorName = $"{person.LastName}, {person.FirstName}";
                }
            }
        }
    }

    private static string GetTermName(int termCode)
    {
        var termName = TermCodeService.GetTermCodeDescription(termCode);
        return termName.StartsWith("Unknown Term") ? $"Term {termCode}" : termName;
    }

    private static string? SerializeChanges(object? oldValues, object? newValues)
    {
        if (oldValues == null && newValues == null)
        {
            return null;
        }

        var changes = new Dictionary<string, ChangeDetail>();

        if (oldValues != null && newValues != null)
        {
            var oldDict = ObjectToDictionary(oldValues);
            var newDict = ObjectToDictionary(newValues);

            foreach (var key in oldDict.Keys.Union(newDict.Keys))
            {
                var oldVal = oldDict.GetValueOrDefault(key);
                var newVal = newDict.GetValueOrDefault(key);

                if (!Equals(oldVal, newVal))
                {
                    changes[key] = new ChangeDetail
                    {
                        OldValue = oldVal?.ToString(),
                        NewValue = newVal?.ToString()
                    };
                }
            }
        }
        else if (newValues != null)
        {
            var newDict = ObjectToDictionary(newValues);
            foreach (var kvp in newDict)
            {
                changes[kvp.Key] = new ChangeDetail
                {
                    OldValue = null,
                    NewValue = kvp.Value?.ToString()
                };
            }
        }
        else
        {
            var oldDict = ObjectToDictionary(oldValues!);
            foreach (var kvp in oldDict)
            {
                changes[kvp.Key] = new ChangeDetail
                {
                    OldValue = kvp.Value?.ToString(),
                    NewValue = null
                };
            }
        }

        return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
    }

    private static Dictionary<string, object?> ObjectToDictionary(object obj)
    {
        return obj.GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(obj));
    }

    private int GetCurrentPersonId()
    {
        var user = _userHelper.GetCurrentUser();
        if (user != null && user.AaudUserId > 0)
        {
            return user.AaudUserId;
        }

        throw new InvalidOperationException("Unable to determine current user for audit attribution.");
    }

    private (string? ipAddress, string? userAgent) GetRequestInfo()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return (null, null);
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();

        return (ipAddress, userAgent);
    }
}
