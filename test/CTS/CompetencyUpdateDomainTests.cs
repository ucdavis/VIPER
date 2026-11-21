using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Controllers;
using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.test.CTS;

public sealed class CompetencyUpdateDomainTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly CompetencyController _controller;

    public CompetencyUpdateDomainTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _context.Domains.AddRange(
            new Domain { DomainId = 1, Name = "First", Order = 1 },
            new Domain { DomainId = 2, Name = "Second", Order = 2 });
        _context.Competencies.Add(new Competency { CompetencyId = 1, DomainId = 1, Number = "1.1", Name = "One", Order = 1 });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        _controller = new CompetencyController(_context);
    }

    public void Dispose() => _context.Dispose();

    private static CompetencyAddUpdate UpdateTo(int domainId) => new()
    {
        CompetencyId = 1,
        DomainId = domainId,
        Number = "1.1",
        Name = "One",
        CanLinkToStudent = false,
    };

    private async Task<int> StoredDomain()
    {
        _context.ChangeTracker.Clear();
        var comp = await _context.Competencies.AsNoTracking()
            .SingleAsync(c => c.CompetencyId == 1, TestContext.Current.CancellationToken);
        return comp.DomainId;
    }

    [Fact]
    public async Task Update_NewDomain_IsSavedAndReturned()
    {
        var result = await _controller.UpdateComptency(1, UpdateTo(2));

        Assert.Equal(2, result.Value!.DomainId);
        Assert.Equal("Second", result.Value.DomainName);
        Assert.Equal(2, await StoredDomain());
    }

    [Fact]
    public async Task Update_UnknownDomain_Returns400WithoutSaving()
    {
        var result = await _controller.UpdateComptency(1, UpdateTo(999));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(1, await StoredDomain());
    }

    [Fact]
    public async Task Add_UnknownDomain_Returns400WithoutSaving()
    {
        var add = UpdateTo(999);
        add.CompetencyId = null;
        add.Number = "9";

        var result = await _controller.AddCompetency(add);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(await _context.Competencies.AnyAsync(c => c.Number == "9", TestContext.Current.CancellationToken));
    }
}
