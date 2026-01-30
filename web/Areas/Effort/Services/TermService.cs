using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for term-related operations.
/// </summary>
public class TermService : ITermService
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly IEffortAuditService _auditService;
    private readonly IMapper _mapper;

    public TermService(EffortDbContext context, VIPERContext viperContext, IEffortAuditService auditService, IMapper mapper)
    {
        _context = context;
        _viperContext = viperContext;
        _auditService = auditService;
        _mapper = mapper;
    }

    public async Task<List<TermDto>> GetTermsAsync(CancellationToken ct = default)
    {
        var terms = await _context.Terms
            .AsNoTracking()
            .OrderByDescending(t => t.TermCode)
            .ToListAsync(ct);

        var termCodes = terms.Select(t => t.TermCode).ToList();

        // Get term codes that have related data (cannot be deleted)
        var termsWithPersons = await _context.Persons
            .Where(p => termCodes.Contains(p.TermCode))
            .Select(p => p.TermCode)
            .Distinct()
            .ToListAsync(ct);
        var termsWithCourses = await _context.Courses
            .Where(c => termCodes.Contains(c.TermCode))
            .Select(c => c.TermCode)
            .Distinct()
            .ToListAsync(ct);
        var termsWithRecords = await _context.Records
            .Where(r => termCodes.Contains(r.TermCode))
            .Select(r => r.TermCode)
            .Distinct()
            .ToListAsync(ct);

        var termsWithData = termsWithPersons
            .Union(termsWithCourses)
            .Union(termsWithRecords)
            .ToHashSet();

        return terms.Select(t =>
        {
            var dto = _mapper.Map<TermDto>(t);
            dto.TermName = GetTermName(t.TermCode);
            dto.CanDelete = !termsWithData.Contains(t.TermCode);
            return dto;
        }).ToList();
    }

    public async Task<TermDto?> GetTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermCode == termCode, ct);

        if (term == null) return null;
        var dto = _mapper.Map<TermDto>(term);
        dto.TermName = GetTermName(termCode);
        return dto;
    }

    public async Task<TermDto?> GetCurrentTermAsync(CancellationToken ct = default)
    {
        // Open = OpenedDate IS NOT NULL AND ClosedDate IS NULL (matches legacy logic)
        var term = await _context.Terms
            .AsNoTracking()
            .Where(t => t.OpenedDate != null && t.ClosedDate == null)
            .OrderByDescending(t => t.TermCode)
            .FirstOrDefaultAsync(ct);

        if (term == null) return null;
        var dto = _mapper.Map<TermDto>(term);
        dto.TermName = GetTermName(term.TermCode);
        return dto;
    }

    public string GetTermName(int termCode)
    {
        var termName = TermCodeService.GetTermCodeDescription(termCode);
        return termName.StartsWith("Unknown Term") ? $"Term {termCode}" : termName;
    }

    // Term Management Operations

    public async Task<TermDto> CreateTermAsync(int termCode, CancellationToken ct = default)
    {
        var existingTerm = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (existingTerm != null)
        {
            throw new InvalidOperationException($"Term {termCode} already exists");
        }

        // New term starts with no dates set (Status will be computed as "Created")
        var term = new Models.Entities.EffortTerm
        {
            TermCode = termCode
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.Terms.Add(term);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.CreateTerm,
            null,
            new { Status = term.Status });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<TermDto>(term);
        dto.TermName = GetTermName(termCode);
        return dto;
    }

    public async Task<bool> DeleteTermAsync(int termCode, CancellationToken ct = default)
    {
        if (!await CanDeleteTermAsync(termCode, ct))
        {
            return false;
        }

        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return false;
        }

        var oldStatus = term.Status;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.Terms.Remove(term);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.DeleteTerm,
            new { Status = oldStatus },
            null);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }

    public async Task<TermDto?> OpenTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        // Can only open if not yet opened (OpenedDate is null)
        if (term.OpenedDate.HasValue)
        {
            throw new InvalidOperationException("Term must be in Created or Harvested status to open");
        }

        var oldStatus = term.Status;
        term.OpenedDate = DateTime.Now;
        term.ClosedDate = null;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.OpenTerm,
            new { Status = oldStatus },
            new { Status = term.Status, term.OpenedDate });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<TermDto>(term);
        dto.TermName = GetTermName(termCode);
        return dto;
    }

    public async Task<(bool Success, string? ErrorMessage)> CloseTermAsync(int termCode, CancellationToken ct = default)
    {
        var (canClose, zeroEnrollmentCount) = await CanCloseTermAsync(termCode, ct);
        if (!canClose)
        {
            return (false, $"Cannot close term: {zeroEnrollmentCount} course(s) have zero enrollment");
        }

        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return (false, "Term not found");
        }

        // Can only close if currently open (OpenedDate set, ClosedDate not set)
        if (!term.OpenedDate.HasValue || term.ClosedDate.HasValue)
        {
            return (false, "Term must be opened before it can be closed");
        }

        var oldStatus = term.Status;
        term.ClosedDate = DateTime.Now;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.CloseTerm,
            new { Status = oldStatus },
            new { Status = term.Status, term.ClosedDate });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return (true, null);
    }

    public async Task<TermDto?> ReopenTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        // Can only reopen if currently closed (ClosedDate is set)
        if (!term.ClosedDate.HasValue)
        {
            throw new InvalidOperationException("Only closed terms can be reopened");
        }

        var oldStatus = term.Status;
        term.ClosedDate = null;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.ReopenTerm,
            new { Status = oldStatus },
            new { Status = term.Status });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<TermDto>(term);
        dto.TermName = GetTermName(termCode);
        return dto;
    }

    public async Task<TermDto?> UnopenTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        // Can only unopen if currently open (OpenedDate set, ClosedDate not set)
        if (!term.OpenedDate.HasValue || term.ClosedDate.HasValue)
        {
            throw new InvalidOperationException("Only opened terms can be reverted to unopened");
        }

        var oldStatus = term.Status;
        // Clear dates to revert - Status will compute based on remaining dates
        term.OpenedDate = null;
        term.ClosedDate = null;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.UnopenTerm,
            new { Status = oldStatus },
            new { Status = term.Status });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<TermDto>(term);
        dto.TermName = GetTermName(termCode);
        return dto;
    }

    public async Task<bool> CanDeleteTermAsync(int termCode, CancellationToken ct = default)
    {
        var hasPersons = await _context.Persons.AnyAsync(p => p.TermCode == termCode, ct);
        if (hasPersons) return false;

        var hasCourses = await _context.Courses.AnyAsync(c => c.TermCode == termCode, ct);
        if (hasCourses) return false;

        var hasRecords = await _context.Records.AnyAsync(r => r.TermCode == termCode, ct);
        if (hasRecords) return false;

        return true;
    }

    public async Task<(bool CanClose, int ZeroEnrollmentCount)> CanCloseTermAsync(int termCode, CancellationToken ct = default)
    {
        var zeroEnrollmentCount = await _context.Courses
            .CountAsync(c => c.TermCode == termCode && c.Enrollment == 0, ct);

        return (zeroEnrollmentCount == 0, zeroEnrollmentCount);
    }

    public async Task<List<AvailableTermDto>> GetAvailableTermsAsync(CancellationToken ct = default)
    {
        var existingTermCodes = await _context.Terms
            .Select(t => t.TermCode)
            .ToListAsync(ct);

        var availableTerms = await _viperContext.Terms
            .AsNoTracking()
            .Where(t => t.StartDate > DateTime.Today)
            .Where(t => !existingTermCodes.Contains(t.TermCode))
            .Where(t => t.TermCode != Viper.Models.VIPER.Term.FacilityScheduleTermCode)
            .OrderBy(t => t.TermCode)
            .Select(t => new AvailableTermDto
            {
                TermCode = t.TermCode,
                TermName = t.Description,
                StartDate = t.StartDate
            })
            .ToListAsync(ct);

        return availableTerms;
    }
}
