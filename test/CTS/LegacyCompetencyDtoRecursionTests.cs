using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Controllers;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.test.CTS;

/// <summary>
/// The legacy list maps competencies too, so a parent cycle must not overflow the stack here
/// either. A regression crashes the test host rather than failing an assertion.
/// </summary>
public sealed class LegacyCompetencyDtoRecursionTests : IDisposable
{
    private readonly VIPERContext _context;

    public LegacyCompetencyDtoRecursionTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Get_SelfParentInData_ReturnsList()
    {
        var token = TestContext.Current.CancellationToken;
        _context.Domains.Add(new Domain { DomainId = 1, Name = "Domain", Order = 1 });
        var comp = new Competency { CompetencyId = 1, DomainId = 1, Number = "1", Name = "One", Order = 1 };
        _context.Competencies.Add(comp);
        _context.LegacyCompetencies.Add(new LegacyCompetency { DvmCompetencyId = 1, DvmCompetencyName = "Legacy" });
        _context.CompetencyMappings.Add(new CompetencyMapping { CompetencyMappingId = 1, CompetencyId = 1, DvmCompetencyId = 1 });
        await _context.SaveChangesAsync(token);
        comp.ParentId = 1;
        await _context.SaveChangesAsync(token);
        _context.ChangeTracker.Clear();

        var result = await new LegacyCompetenciesController(_context).Get();

        var legacy = Assert.Single(result.Value!);
        var mapped = Assert.Single(legacy.Competencies);
        Assert.Equal(1, mapped.CompetencyId);
        Assert.Null(mapped.Parent!.Parent);
    }
}
