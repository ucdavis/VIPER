using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.Students;

/// <summary>
/// Tests for CareerSelectionOptionService: the admin option lists, their ordering, and the usage
/// counts that decide whether an option can be deleted.
/// </summary>
public sealed class CareerSelectionOptionServiceTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly CareerSelectionOptionService _service;
    private int _nextPidm = 10000000;

    public CareerSelectionOptionServiceTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _service = new CareerSelectionOptionService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private CareerSelection Selection(Action<CareerSelection> customize)
    {
        var selection = new CareerSelection
        {
            Pidm = _nextPidm++,
            DateAdded = DateTime.Now,
        };
        customize(selection);
        return selection;
    }

    [Fact]
    public async Task GetOptionsAsync_SortsCatchAllLastThenByLabel()
    {
        _context.CareerOptions.AddRange(
            new CareerOption { CareerOptionId = 1, Career = "Other", IsOther = true },
            new CareerOption { CareerOptionId = 2, Career = "Small Animal" },
            new CareerOption { CareerOptionId = 3, Career = "Academia" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetOptionsAsync(CareerOptionType.Career, includeUsage: true, TestContext.Current.CancellationToken);

        Assert.Equal(["Academia", "Small Animal", "Other"], result.Select(o => o.Label));
        Assert.True(result[2].IsOther);
        Assert.Equal(3, result[0].Id);
    }

    [Fact]
    public async Task GetOptionsAsync_CountsCareerUsageIncludingZero()
    {
        _context.CareerOptions.AddRange(
            new CareerOption { CareerOptionId = 1, Career = "Academia" },
            new CareerOption { CareerOptionId = 2, Career = "Industry" });
        _context.CareerSelections.AddRange(
            Selection(c => c.Career = 1),
            Selection(c => c.Career = 1),
            Selection(c => c.Career = null));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetOptionsAsync(CareerOptionType.Career, includeUsage: true, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Single(o => o.Id == 1).UsageCount);
        Assert.Equal(0, result.Single(o => o.Id == 2).UsageCount);
    }

    [Fact]
    public async Task GetOptionsAsync_CountsSpeciesFromBothColumns()
    {
        _context.SpeciesOptions.AddRange(
            new SpeciesOption { SpeciesOptionId = 1, Species = "Equine" },
            new SpeciesOption { SpeciesOptionId = 2, Species = "Canine" },
            new SpeciesOption { SpeciesOptionId = 3, Species = "Bovine" });
        _context.CareerSelections.AddRange(
            Selection(c => { c.FirstSpecies = 1; c.SecondSpecies = 2; }),
            // Used only as a secondary species: still blocks a delete.
            Selection(c => { c.FirstSpecies = null; c.SecondSpecies = 3; }),
            // The same species in both columns is one selection, not two.
            Selection(c => { c.FirstSpecies = 1; c.SecondSpecies = 1; }));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetOptionsAsync(CareerOptionType.Species, includeUsage: true, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Single(o => o.Id == 1).UsageCount);
        Assert.Equal(1, result.Single(o => o.Id == 2).UsageCount);
        Assert.Equal(1, result.Single(o => o.Id == 3).UsageCount);
    }

    [Fact]
    public async Task GetOptionsAsync_CountsPostGradUsage()
    {
        _context.PostGradOptions.AddRange(
            new PostGradOption { PostGradOptionId = 1, PostGrad = "Internship" },
            new PostGradOption { PostGradOptionId = 2, PostGrad = "Other", IsOther = true });
        _context.CareerSelections.Add(Selection(c => c.PostGrad = 2));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetOptionsAsync(CareerOptionType.PostGrad, includeUsage: true, TestContext.Current.CancellationToken);

        Assert.Equal(0, result.Single(o => o.Id == 1).UsageCount);
        Assert.Equal(1, result.Single(o => o.Id == 2).UsageCount);
    }

    [Fact]
    public async Task GetOptionsAsync_WithoutUsage_LeavesCountsAtZero()
    {
        _context.CareerOptions.Add(new CareerOption { CareerOptionId = 1, Career = "Academia" });
        _context.CareerSelections.Add(Selection(c => c.Career = 1));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetOptionsAsync(CareerOptionType.Career, includeUsage: false, TestContext.Current.CancellationToken);

        Assert.Equal(0, Assert.Single(result).UsageCount);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task SeedSpeciesAsync()
    {
        _context.SpeciesOptions.AddRange(
            new SpeciesOption { SpeciesOptionId = 1, Species = "Equine" },
            new SpeciesOption { SpeciesOptionId = 2, Species = "Canine" },
            new SpeciesOption { SpeciesOptionId = 3, Species = "Other", IsOther = true });
        await _context.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task CreateOptionAsync_TrimsAndSavesANewOption()
    {
        await SeedSpeciesAsync();

        var result = await _service.CreateOptionAsync(CareerOptionType.Species, "  Exotics  ", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Success, result.Status);
        Assert.Equal("Exotics", result.Option!.Label);
        Assert.False(result.Option.IsOther);
        var saved = await _context.SpeciesOptions.SingleAsync(o => o.SpeciesOptionId == result.Option.Id, Ct);
        Assert.Equal("Exotics", saved.Species);
    }

    [Theory]
    [InlineData("equine")]
    [InlineData("  EQUINE ")]
    [InlineData("other")]
    public async Task CreateOptionAsync_RejectsADuplicateIgnoringCaseAndSpaces(string label)
    {
        await SeedSpeciesAsync();

        var result = await _service.CreateOptionAsync(CareerOptionType.Species, label, Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Conflict, result.Status);
        Assert.Equal(3, await _context.SpeciesOptions.CountAsync(Ct));
    }

    [Fact]
    public async Task CreateOptionAsync_AllowsTheSameNameInADifferentList()
    {
        await SeedSpeciesAsync();

        var result = await _service.CreateOptionAsync(CareerOptionType.Career, "Equine", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Success, result.Status);
    }

    [Theory]
    [InlineData(CareerOptionType.Career, 101, CareerSelectionOptionWriteStatus.Invalid)]
    [InlineData(CareerOptionType.Species, 101, CareerSelectionOptionWriteStatus.Invalid)]
    [InlineData(CareerOptionType.PostGrad, 150, CareerSelectionOptionWriteStatus.Success)]
    [InlineData(CareerOptionType.PostGrad, 201, CareerSelectionOptionWriteStatus.Invalid)]
    public async Task CreateOptionAsync_HoldsNamesToTheirListsLength(CareerOptionType type, int length, CareerSelectionOptionWriteStatus expected)
    {
        var result = await _service.CreateOptionAsync(type, new string('a', length), Ct);

        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task CreateOptionAsync_RejectsABlankName()
    {
        var result = await _service.CreateOptionAsync(CareerOptionType.Career, "   ", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task UpdateOptionAsync_RenamesAnOption()
    {
        await SeedSpeciesAsync();

        var result = await _service.UpdateOptionAsync(CareerOptionType.Species, 1, " Horses ", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Success, result.Status);
        Assert.Equal("Horses", (await _context.SpeciesOptions.SingleAsync(o => o.SpeciesOptionId == 1, Ct)).Species);
    }

    [Fact]
    public async Task UpdateOptionAsync_AllowsChangingOnlyTheCase()
    {
        await SeedSpeciesAsync();

        var result = await _service.UpdateOptionAsync(CareerOptionType.Species, 1, "EQUINE", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Success, result.Status);
        Assert.Equal("EQUINE", result.Option!.Label);
    }

    [Fact]
    public async Task UpdateOptionAsync_RejectsAnotherOptionsName()
    {
        await SeedSpeciesAsync();

        var result = await _service.UpdateOptionAsync(CareerOptionType.Species, 1, "canine", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Conflict, result.Status);
        Assert.Equal("Equine", (await _context.SpeciesOptions.SingleAsync(o => o.SpeciesOptionId == 1, Ct)).Species);
    }

    [Fact]
    public async Task UpdateOptionAsync_RefusesTheCatchAll()
    {
        await SeedSpeciesAsync();

        var result = await _service.UpdateOptionAsync(CareerOptionType.Species, 3, "Something Else", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task UpdateOptionAsync_ReportsAMissingOption()
    {
        await SeedSpeciesAsync();

        var result = await _service.UpdateOptionAsync(CareerOptionType.Species, 99, "Horses", Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteOptionAsync_DeletesAnUnusedOption()
    {
        await SeedSpeciesAsync();

        var result = await _service.DeleteOptionAsync(CareerOptionType.Species, 2, Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Success, result.Status);
        Assert.False(await _context.SpeciesOptions.AnyAsync(o => o.SpeciesOptionId == 2, Ct));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteOptionAsync_RefusesASpeciesInUseInEitherColumn(bool asPrimary)
    {
        await SeedSpeciesAsync();
        _context.CareerSelections.Add(Selection(c =>
        {
            if (asPrimary)
            {
                c.FirstSpecies = 2;
            }
            else
            {
                c.SecondSpecies = 2;
            }
        }));
        await _context.SaveChangesAsync(Ct);

        var result = await _service.DeleteOptionAsync(CareerOptionType.Species, 2, Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Conflict, result.Status);
        Assert.True(await _context.SpeciesOptions.AnyAsync(o => o.SpeciesOptionId == 2, Ct));
    }

    [Fact]
    public async Task DeleteOptionAsync_RefusesTheCatchAll()
    {
        await SeedSpeciesAsync();

        var result = await _service.DeleteOptionAsync(CareerOptionType.Species, 3, Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task DeleteOptionAsync_ReportsAMissingOption()
    {
        var result = await _service.DeleteOptionAsync(CareerOptionType.PostGrad, 99, Ct);

        Assert.Equal(CareerSelectionOptionWriteStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData("career", CareerOptionType.Career)]
    [InlineData("species", CareerOptionType.Species)]
    [InlineData("post-grad", CareerOptionType.PostGrad)]
    [InlineData("Post-Grad", CareerOptionType.PostGrad)]
    public void TryParseSlug_ResolvesKnownSlugs(string slug, CareerOptionType expected)
    {
        Assert.True(CareerOptionTypes.TryParseSlug(slug, out var type));
        Assert.Equal(expected, type);
    }

    [Theory]
    [InlineData("postGrad")]
    [InlineData("PostGrad")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseSlug_RejectsUnknownSlugs(string? slug)
    {
        Assert.False(CareerOptionTypes.TryParseSlug(slug, out _));
    }
}
