using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;

namespace Viper.test.Personnel;

/// <summary>
/// Unit tests for PhoneListService, the entry point every list-scoped request resolves a list
/// through. Lookup is by Code rather than Name so that renaming a list for display cannot
/// break the routes and API paths that address it.
/// </summary>
public sealed class PhoneListServiceTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneListService _service;

    public PhoneListServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);
        _service = new PhoneListService(_context);
    }

    public void Dispose() => _context.Dispose();

    private void SeedLists()
    {
        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 1,
            Code = "VMDO",
            Name = "Dean's Office",
            MaintainRole = "SVMSecure.PhoneLists.VMDOMaintain",
        });
        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 2,
            Code = "OTHER",
            Name = "Some Other Unit",
            MaintainRole = "SVMSecure.PhoneLists.OtherMaintain",
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetListByCode_ReturnsMatchingList()
    {
        SeedLists();

        var result = await _service.GetListByCode("OTHER", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.PhoneListId);
        Assert.Equal("Some Other Unit", result.Name);
    }

    [Fact]
    public async Task GetListByCode_ResolvesIndependentlyOfDisplayName()
    {
        SeedLists();
        var list = await _context.PhoneList.SingleAsync(l => l.Code == "VMDO", TestContext.Current.CancellationToken);
        list.Name = "Office of the Dean";
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetListByCode("VMDO", TestContext.Current.CancellationToken);

        Assert.Equal(1, result.PhoneListId);
    }

    [Fact]
    public async Task GetListByCode_Throws_WhenCodeNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetListByCode("NOPE", TestContext.Current.CancellationToken));
    }
}
