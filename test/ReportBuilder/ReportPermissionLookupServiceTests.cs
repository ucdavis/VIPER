using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.test.ReportBuilder;

/// <summary>The admin form picks view permissions from RAPS, and saves only names RAPS has.</summary>
public sealed class ReportPermissionLookupServiceTests : IDisposable
{
    private readonly RAPSContext _raps;
    private readonly ReportPermissionLookupService _service;

    public ReportPermissionLookupServiceTests()
    {
        DbContextOptions<RAPSContext> options = new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _raps = new RAPSContext(options);
        _service = new ReportPermissionLookupService(_raps);
        foreach (string name in new[] { "SVMSecure.Personnel", "SVMSecure.Effort", "SVMSecure.Personnel.Reports", "SVMSecure.CMS" })
        {
            _raps.TblPermissions.Add(new TblPermission { Permission = name, Description = $"About {name}" });
        }

        _raps.SaveChanges();
    }

    public void Dispose()
    {
        _raps.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_FindsNamesContainingTheTextInOrder()
    {
        IReadOnlyList<ReportPermissionOption> found = await _service.SearchAsync(" Personnel ", Ct);

        Assert.Equal(
            [
                new ReportPermissionOption { Permission = "SVMSecure.Personnel", Description = "About SVMSecure.Personnel" },
                new ReportPermissionOption { Permission = "SVMSecure.Personnel.Reports", Description = "About SVMSecure.Personnel.Reports" },
            ],
            found);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Search_WithoutText_ListsThemAll(string? text)
    {
        IReadOnlyList<ReportPermissionOption> found = await _service.SearchAsync(text, Ct);

        Assert.Equal(["SVMSecure.CMS", "SVMSecure.Effort", "SVMSecure.Personnel", "SVMSecure.Personnel.Reports"], found.Select(p => p.Permission));
    }

    [Fact]
    public async Task Search_ReturnsAtMostFifty()
    {
        for (int i = 0; i < 60; i++)
        {
            _raps.TblPermissions.Add(new TblPermission { Permission = $"SVMSecure.Many.{i:D2}" });
        }

        await _raps.SaveChangesAsync(Ct);

        Assert.Equal(ReportPermissionLookupService.MaxResults, (await _service.SearchAsync("Many", Ct)).Count);
    }

    [Fact]
    public async Task Find_ReturnsTheNamesRapsHas()
    {
        IReadOnlyDictionary<string, string> found = await _service.FindAsync(
            ["SVMSecure.Effort", " SVMSecure.CMS ", "SVMSecure.Typo", " "], Ct);

        Assert.Equal(["SVMSecure.CMS", "SVMSecure.Effort"], found.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Find_NoNames_IsEmpty()
    {
        Assert.Empty(await _service.FindAsync([], Ct));
    }
}
