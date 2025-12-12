using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Effort.Extensions;
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

    public TermService(EffortDbContext context, VIPERContext viperContext)
    {
        _context = context;
        _viperContext = viperContext;
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
            var dto = t.ToDto(GetTermName(t.TermCode));
            dto.CanDelete = !termsWithData.Contains(t.TermCode);
            return dto;
        }).ToList();
    }

    public async Task<TermDto?> GetTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermCode == termCode, ct);

        return term?.ToDto(GetTermName(termCode));
    }

    public async Task<TermDto?> GetCurrentTermAsync(CancellationToken ct = default)
    {
        var term = await _context.Terms
            .AsNoTracking()
            .Where(t => t.Status == "Opened")
            .OrderByDescending(t => t.TermCode)
            .FirstOrDefaultAsync(ct);

        return term?.ToDto(GetTermName(term.TermCode));
    }

    public string GetTermName(int termCode)
    {
        var termName = TermCodeService.GetTermCodeDescription(termCode);
        return termName.StartsWith("Unknown Term") ? $"Term {termCode}" : termName;
    }

    // Term Management Operations

    public async Task<TermDto> CreateTermAsync(int termCode, int modifiedBy, CancellationToken ct = default)
    {
        var existingTerm = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (existingTerm != null)
        {
            throw new InvalidOperationException($"Term {termCode} already exists");
        }

        var term = new Models.Entities.EffortTerm
        {
            TermCode = termCode,
            Status = "Created",
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            ModifiedBy = modifiedBy
        };

        _context.Terms.Add(term);
        await _context.SaveChangesAsync(ct);

        return term.ToDto(GetTermName(termCode));
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

        _context.Terms.Remove(term);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TermDto?> OpenTermAsync(int termCode, int modifiedBy, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        if (term.Status is not ("Created" or "Harvested"))
        {
            throw new InvalidOperationException("Term must be in Created or Harvested status to open");
        }

        term.Status = "Opened";
        term.OpenedDate = DateTime.Now;
        term.ClosedDate = null;
        term.ModifiedDate = DateTime.Now;
        term.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync(ct);
        return term.ToDto(GetTermName(termCode));
    }

    public async Task<(bool Success, string? ErrorMessage)> CloseTermAsync(int termCode, int modifiedBy, CancellationToken ct = default)
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

        if (term.Status != "Opened")
        {
            return (false, "Term must be opened before it can be closed");
        }

        term.Status = "Closed";
        term.ClosedDate = DateTime.Now;
        term.ModifiedDate = DateTime.Now;
        term.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<TermDto?> ReopenTermAsync(int termCode, int modifiedBy, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        if (term.Status != "Closed")
        {
            throw new InvalidOperationException("Only closed terms can be reopened");
        }

        term.Status = "Opened";
        term.ClosedDate = null;
        term.ModifiedDate = DateTime.Now;
        term.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync(ct);
        return term.ToDto(GetTermName(termCode));
    }

    public async Task<TermDto?> UnopenTermAsync(int termCode, int modifiedBy, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        if (term.Status != "Opened")
        {
            throw new InvalidOperationException("Only opened terms can be reverted to unopened");
        }

        // Revert to previous status based on whether it was harvested
        term.Status = term.HarvestedDate.HasValue ? "Harvested" : "Created";
        term.OpenedDate = null;
        term.ClosedDate = null;
        term.ModifiedDate = DateTime.Now;
        term.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync(ct);
        return term.ToDto(GetTermName(termCode));
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
