using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

public class SabbaticalService : ISabbaticalService
{
    private readonly EffortDbContext _context;
    private readonly ILogger<SabbaticalService> _logger;

    public SabbaticalService(EffortDbContext context, ILogger<SabbaticalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetPersonDepartmentAsync(int personId, CancellationToken ct = default)
    {
        var effortPerson = await _context.Persons
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .OrderByDescending(p => p.TermCode)
            .Select(p => p.EffortDept)
            .FirstOrDefaultAsync(ct);

        return effortPerson;
    }

    public async Task<SabbaticalDto?> GetByPersonIdAsync(int personId, CancellationToken ct = default)
    {
        var entity = await _context.Sabbaticals
            .AsNoTracking()
            .Include(s => s.ModifiedByPerson)
            .FirstOrDefaultAsync(s => s.PersonId == personId, ct);

        if (entity == null)
        {
            return null;
        }

        return MapToDto(entity);
    }

    public async Task<SabbaticalDto> SaveAsync(
        int personId,
        string? excludeClinicalTerms,
        string? excludeDidacticTerms,
        int modifiedBy,
        CancellationToken ct = default)
    {
        var entity = await _context.Sabbaticals
            .Include(s => s.ModifiedByPerson)
            .FirstOrDefaultAsync(s => s.PersonId == personId, ct);

        if (entity == null)
        {
            entity = new Sabbatical
            {
                PersonId = personId,
                ExcludeClinicalTerms = excludeClinicalTerms,
                ExcludeDidacticTerms = excludeDidacticTerms,
                ModifiedDate = DateTime.Now,
                ModifiedBy = modifiedBy
            };
            _context.Sabbaticals.Add(entity);
        }
        else
        {
            entity.ExcludeClinicalTerms = excludeClinicalTerms;
            entity.ExcludeDidacticTerms = excludeDidacticTerms;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedBy = modifiedBy;
        }

        await _context.SaveChangesAsync(ct);

        // Reload navigation property for the response
        await _context.Entry(entity).Reference(s => s.ModifiedByPerson).LoadAsync(ct);

        _logger.LogInformation(
            "Saved sabbatical data for PersonId={PersonId}, ModifiedBy={ModifiedBy}",
            personId, modifiedBy);

        return MapToDto(entity);
    }

    private static SabbaticalDto MapToDto(Sabbatical entity)
    {
        return new SabbaticalDto
        {
            PersonId = entity.PersonId,
            ExcludeClinicalTerms = entity.ExcludeClinicalTerms,
            ExcludeDidacticTerms = entity.ExcludeDidacticTerms,
            ModifiedDate = entity.ModifiedDate,
            ModifiedBy = entity.ModifiedByPerson != null
                ? $"{entity.ModifiedByPerson.FirstName} {entity.ModifiedByPerson.LastName}"
                : null
        };
    }
}
