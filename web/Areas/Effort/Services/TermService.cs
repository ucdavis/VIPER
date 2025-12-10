using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Effort.Extensions;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for term-related operations.
/// </summary>
public class TermService : ITermService
{
    private readonly EffortDbContext _context;

    public TermService(EffortDbContext context)
    {
        _context = context;
    }

    public async Task<List<TermDto>> GetTermsAsync(CancellationToken ct = default)
    {
        var terms = await _context.Terms
            .AsNoTracking()
            .OrderByDescending(t => t.TermCode)
            .ToListAsync(ct);

        return terms.Select(t => t.ToDto(GetTermName(t.TermCode))).ToList();
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
}
