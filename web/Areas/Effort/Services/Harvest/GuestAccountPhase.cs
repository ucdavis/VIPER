using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Phase 4: Guest Account import.
/// Imports department-level guest accounts (APCGUEST, PHRGUEST, etc.) for effort tracking.
/// </summary>
public sealed class GuestAccountPhase : HarvestPhaseBase
{
    /// <inheritdoc />
    public override string PhaseName => "Guest Accounts";

    /// <inheritdoc />
    public override int Order => 40;

    /// <inheritdoc />
    public override async Task GeneratePreviewAsync(HarvestContext context, CancellationToken ct = default)
    {
        // Look up guest account PersonIds from users.Person by MothraId
        var guestPersons = await context.ViperContext.People
            .AsNoTracking()
            .Where(p => EffortConstants.GuestAccountIds.Contains(p.MothraId))
            .Select(p => new { p.MothraId, p.PersonId })
            .ToListAsync(ct);

        var personIdLookup = guestPersons.ToDictionary(p => p.MothraId, p => p.PersonId);

        foreach (var guestId in EffortConstants.GuestAccountIds)
        {
            if (!personIdLookup.TryGetValue(guestId, out var personId))
            {
                context.Logger.LogWarning(
                    "Guest account {GuestId} not found in users.Person",
                    LogSanitizer.SanitizeId(guestId));
                continue;
            }

            var dept = guestId[..3]; // First 3 chars are department code

            context.Preview.GuestAccounts.Add(new HarvestPersonPreview
            {
                MothraId = guestId,
                PersonId = personId,
                FullName = $"{dept}, GUEST",
                FirstName = "GUEST",
                LastName = dept,
                Department = dept,
                TitleCode = "",
                Source = EffortConstants.SourceGuest
            });
        }
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(HarvestContext context, CancellationToken ct = default)
    {
        context.Logger.LogInformation(
            "Importing {Count} guest accounts for term {TermCode}",
            context.Preview.GuestAccounts.Count,
            context.TermCode);

        foreach (var guest in context.Preview.GuestAccounts)
        {
            await ImportPersonAsync(guest, context, ct);
        }
    }
}
