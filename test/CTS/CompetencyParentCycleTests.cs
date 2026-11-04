using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Controllers;
using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.test.CTS;

/// <summary>
/// A cycle in the parent chain used to overflow the stack in CompetencyDto, which kills the
/// process rather than failing the request. A regression here crashes the test host, so expect
/// that instead of a failed assertion. A real context is needed because the recursion comes
/// from EF relationship fix-up, which the mocks in CompetencyControllerTest do not perform.
/// </summary>
public sealed class CompetencyParentCycleTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly CompetencyController _controller;

    public CompetencyParentCycleTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _controller = new CompetencyController(_context);
    }

    public void Dispose() => _context.Dispose();

    /// <summary>Seeds the chain 1 > 2 > 3 (parent first), plus an unrelated root 4.</summary>
    private async Task SeedChain()
    {
        var token = TestContext.Current.CancellationToken;
        _context.Domains.Add(new Domain { DomainId = 1, Name = "Domain", Order = 1 });
        _context.Competencies.AddRange(
            new Competency { CompetencyId = 1, DomainId = 1, Number = "1", Name = "One", Order = 1 },
            new Competency { CompetencyId = 2, DomainId = 1, Number = "1.1", Name = "Two", Order = 2, ParentId = 1 },
            new Competency { CompetencyId = 3, DomainId = 1, Number = "1.1.1", Name = "Three", Order = 3, ParentId = 2 },
            new Competency { CompetencyId = 4, DomainId = 1, Number = "2", Name = "Four", Order = 4 });
        await _context.SaveChangesAsync(token);
        _context.ChangeTracker.Clear();
    }

    /// <summary>Writes a parent directly, the way bad data already in the table would look.</summary>
    private async Task SetParentInData(int competencyId, int parentId)
    {
        var token = TestContext.Current.CancellationToken;
        var comp = await _context.Competencies.SingleAsync(c => c.CompetencyId == competencyId, token);
        comp.ParentId = parentId;
        await _context.SaveChangesAsync(token);
        _context.ChangeTracker.Clear();
    }

    private static CompetencyAddUpdate Update(int id, string number, int? parentId) => new()
    {
        CompetencyId = id,
        DomainId = 1,
        Number = number,
        Name = "Updated",
        CanLinkToStudent = false,
        ParentId = parentId,
    };

    private async Task<int?> StoredParent(int competencyId)
    {
        _context.ChangeTracker.Clear();
        var comp = await _context.Competencies.AsNoTracking()
            .SingleAsync(c => c.CompetencyId == competencyId, TestContext.Current.CancellationToken);
        return comp.ParentId;
    }

    [Fact]
    public async Task Update_OwnIdAsParent_Returns400WithoutSaving()
    {
        await SeedChain();

        var result = await _controller.UpdateComptency(4, Update(4, "2", 4));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(await StoredParent(4));
    }

    [Theory]
    [InlineData(2)] // child
    [InlineData(3)] // child of child
    public async Task Update_DescendantAsParent_Returns400WithoutSaving(int descendantId)
    {
        await SeedChain();

        var result = await _controller.UpdateComptency(1, Update(1, "1", descendantId));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(await StoredParent(1));
    }

    [Fact]
    public async Task Update_NonDescendantAsParent_Saves()
    {
        await SeedChain();

        var result = await _controller.UpdateComptency(3, Update(3, "1.1.1", 4));

        Assert.Equal(4, result.Value!.ParentId);
        Assert.Equal(4, await StoredParent(3));
    }

    [Fact]
    public async Task Update_ClearingParent_Saves()
    {
        await SeedChain();

        var result = await _controller.UpdateComptency(3, Update(3, "1.1.1", null));

        Assert.Null(result.Value!.ParentId);
        Assert.Null(await StoredParent(3));
    }

    [Fact]
    public async Task Update_NonexistentParent_Returns400WithoutSaving()
    {
        await SeedChain();

        var result = await _controller.UpdateComptency(4, Update(4, "2", 999));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(await StoredParent(4));
    }

    [Fact]
    public async Task Add_NonexistentParent_Returns400WithoutSaving()
    {
        await SeedChain();
        var add = Update(0, "9", 999);
        add.CompetencyId = null;

        var result = await _controller.AddCompetency(add);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(await _context.Competencies.AnyAsync(c => c.Number == "9", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Add_ExistingParent_Saves()
    {
        await SeedChain();
        var add = Update(0, "9", 4);
        add.CompetencyId = null;

        var result = await _controller.AddCompetency(add);

        Assert.Equal(4, await StoredParent(result.Value!.CompetencyId));
    }

    [Fact]
    public async Task Add_ParentChainAlreadyCyclic_Returns400WithoutSaving()
    {
        await SeedChain();
        await SetParentInData(1, 2); // 1 and 2 now point at each other
        var add = Update(0, "9", 1);
        add.CompetencyId = null;

        var result = await _controller.AddCompetency(add);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(await _context.Competencies.AnyAsync(c => c.Number == "9", TestContext.Current.CancellationToken));
    }

    /// <summary>The walk must stop on an existing cycle, and refuse to attach anything to it.</summary>
    [Fact]
    public async Task Update_ParentChainAlreadyCyclic_Returns400WithoutSaving()
    {
        await SeedChain();
        await SetParentInData(1, 2); // 1 and 2 now point at each other

        var result = await _controller.UpdateComptency(4, Update(4, "2", 1));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(await StoredParent(4));
    }

    [Fact]
    public async Task Index_SelfParentInData_ReturnsList()
    {
        await SeedChain();
        await SetParentInData(4, 4);

        var result = await _controller.Index();

        Assert.Equal(4, result.Value!.Count);
    }

    [Fact]
    public async Task Index_TwoNodeCycleInData_ReturnsList()
    {
        await SeedChain();
        await SetParentInData(1, 2);

        var result = await _controller.Index();

        Assert.Equal(4, result.Value!.Count);
    }

    [Fact]
    public async Task GetComp_SelfParentInData_ReturnsCompetency()
    {
        await SeedChain();
        await SetParentInData(4, 4);

        var result = await _controller.GetComp(4);

        Assert.Equal(4, result.Value!.CompetencyId);
    }

    [Fact]
    public void Dto_IncludesParentButNotGrandparent()
    {
        var grandparent = new Competency { CompetencyId = 1, Number = "1", Name = "One" };
        var parent = new Competency { CompetencyId = 2, Number = "1.1", Name = "Two", Parent = grandparent };
        var child = new Competency { CompetencyId = 3, Number = "1.1.1", Name = "Three", Parent = parent };

        var dto = new CompetencyDto(child);

        Assert.Equal(2, dto.Parent!.CompetencyId);
        Assert.Null(dto.Parent.Parent);
    }
}
