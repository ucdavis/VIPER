using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;

namespace Viper.test.Personnel;

/// <summary>
/// Unit tests for PhoneSVMSectionService, covering the SQL Server 2016-safe null-last
/// SortOrder ordering convention shared with PhoneListUnitService and
/// PhoneSVMFrequentNumberService.
/// </summary>
public sealed class PhoneSVMSectionServiceTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneSVMSectionService _service;

    public PhoneSVMSectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);
        _service = new PhoneSVMSectionService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetSVMSections_ReturnsEmptyList_WhenNoSectionsExist()
    {
        var results = await _service.GetSVMSections(TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetSVMSections_OrdersRowsWithNoSortOrderLast()
    {
        _context.SVMSection.AddRange(
            new SVMSection { SectionId = 1, Name = "Zebra Unsorted", SortOrder = null },
            new SVMSection { SectionId = 2, Name = "Registrar", SortOrder = 2 },
            new SVMSection { SectionId = 3, Name = "Dean's Office", SortOrder = 1 }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await _service.GetSVMSections(TestContext.Current.CancellationToken);

        Assert.Equal(["Dean's Office", "Registrar", "Zebra Unsorted"], results.Select(r => r.Name));
    }
}
