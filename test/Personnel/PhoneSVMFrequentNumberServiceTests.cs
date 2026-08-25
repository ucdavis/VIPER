using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Unit tests for PhoneSVMFrequentNumberService, focused on the soft-delete
/// convention (rows are marked inactive rather than removed, so ModifiedDate keeps
/// tracking when the list last changed) and the SQL Server 2016-safe null-last
/// SortOrder ordering.
/// </summary>
public sealed class PhoneSVMFrequentNumberServiceTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly IUserHelper _userHelper;
    private readonly PhoneSVMFrequentNumberService _service;

    private const string CallerIam = "caller01";

    public PhoneSVMFrequentNumberServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);

        _userHelper = Substitute.For<IUserHelper>();
        _userHelper.GetCurrentUser().Returns(new AaudUser
        {
            ClientId = "ucd.edu",
            MothraId = CallerIam,
            LastName = "Caller",
            FirstName = "Test",
            DisplayLastName = "Caller",
            DisplayFirstName = "Test",
            DisplayFullName = "Test Caller",
            IamId = CallerIam,
        });

        _service = new PhoneSVMFrequentNumberService(_context, _userHelper);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task AddFrequentNumber_SetsIsActiveTrue_AndModifiedMetadata()
    {
        var request = new SVMFrequentNumberRequest { Label = "Front Desk", Phone = "530-555-1000" };

        await _service.AddFrequentNumber(request, TestContext.Current.CancellationToken);

        var saved = await _context.SVMFrequentNumber
            .SingleAsync(n => n.Label == "Front Desk", TestContext.Current.CancellationToken);
        Assert.True(saved.IsActive);
        Assert.Equal(CallerIam, saved.ModifiedBy);
        Assert.NotNull(saved.ModifiedDate);
    }

    [Fact]
    public async Task DeleteFrequentNumber_SoftDeletes_ExcludesRowFromGetSVMFrequentNumbers()
    {
        _context.SVMFrequentNumber.Add(new SVMFrequentNumber
        {
            NumberId = 1,
            Label = "Pharmacy",
            Phone = "530-555-2000",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteFrequentNumber(1, TestContext.Current.CancellationToken);

        var stillExists = await _context.SVMFrequentNumber
            .FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.NotNull(stillExists);
        Assert.False(stillExists.IsActive);

        var activeNumbers = await _service.GetSVMFrequentNumbers(TestContext.Current.CancellationToken);
        Assert.Empty(activeNumbers);
    }

    [Fact]
    public async Task DeleteFrequentNumber_Throws_WhenAlreadyInactive()
    {
        _context.SVMFrequentNumber.Add(new SVMFrequentNumber
        {
            NumberId = 1,
            Label = "Retired Line",
            Phone = "530-555-3000",
            IsActive = false,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteFrequentNumber(1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSVMFrequentNumbers_OrdersRowsWithNoSortOrderLast()
    {
        _context.SVMFrequentNumber.AddRange(
            new SVMFrequentNumber { NumberId = 1, Label = "Zebra Unsorted", Phone = "1", IsActive = true, SortOrder = null },
            new SVMFrequentNumber { NumberId = 2, Label = "Pharmacy", Phone = "2", IsActive = true, SortOrder = 2 },
            new SVMFrequentNumber { NumberId = 3, Label = "Front Desk", Phone = "3", IsActive = true, SortOrder = 1 }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await _service.GetSVMFrequentNumbers(TestContext.Current.CancellationToken);

        Assert.Equal(["Front Desk", "Pharmacy", "Zebra Unsorted"], results.Select(r => r.Label));
    }
}
