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
    private readonly PhoneSVMFrequentNumberService _service;

    private const string CallerIam = "caller01";

    public PhoneSVMFrequentNumberServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);

        IUserHelper _userHelper = Substitute.For<IUserHelper>();
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

    private void SeedNumber(string label, string phone, bool isActive = true)
    {
        _context.SVMFrequentNumber.Add(new SVMFrequentNumber
        {
            NumberId = 1,
            Label = label,
            Phone = phone,
            IsActive = isActive,
        });
        _context.SaveChanges();
    }

    private async Task<SVMFrequentNumber?> FindNumber(int numberId) =>
        await _context.SVMFrequentNumber.FindAsync(new object?[] { numberId }, TestContext.Current.CancellationToken);

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

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteFrequentNumber(1, TestContext.Current.CancellationToken));

        Assert.Equal("Frequent number is already deleted.", ex.Message);
    }

    // The three ways a lookup by id can fail carry three different messages. They were once all
    // "already deleted", which is only true of the last of them: a maintainer given a bad id, or
    // one for a row that never existed, was told a row had been removed that never was there.
    [Theory]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task DeleteFrequentNumber_Throws_ForAnIdThatCouldNeverBeValid(int entryId)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteFrequentNumber(entryId, TestContext.Current.CancellationToken));

        Assert.Equal("Frequent number id is not valid.", ex.Message);
    }

    // Zero sits on the near side of that guard and is looked up like any other id. NumberId is an
    // identity column starting at 1, so the lookup misses and it reports as the missing row it is.
    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public async Task DeleteFrequentNumber_Throws_WhenNoSuchRowExists(int entryId)
    {
        SeedNumber(label: "Front Desk", phone: "530-555-1000");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteFrequentNumber(entryId, TestContext.Current.CancellationToken));

        Assert.Equal("Frequent number not found.", ex.Message);
    }

    [Fact]
    public async Task UpdateFrequentNumber_OverwritesFields_AndModifiedMetadata()
    {
        SeedNumber(label: "Front Desk", phone: "530-555-1000");
        var request = new SVMFrequentNumberRequest { Label = "Reception", Phone = "530-555-4000" };

        await _service.UpdateFrequentNumber(1, request, TestContext.Current.CancellationToken);

        var saved = await FindNumber(1);
        Assert.NotNull(saved);
        Assert.Equal("Reception", saved.Label);
        Assert.Equal("530-555-4000", saved.Phone);
        Assert.Equal(CallerIam, saved.ModifiedBy);
        Assert.NotNull(saved.ModifiedDate);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task UpdateFrequentNumber_TrimsWhitespace()
    {
        SeedNumber(label: "Front Desk", phone: "530-555-1000");
        var request = new SVMFrequentNumberRequest { Label = "  Reception  ", Phone = "  530-555-4000  " };

        await _service.UpdateFrequentNumber(1, request, TestContext.Current.CancellationToken);

        var saved = await FindNumber(1);
        Assert.NotNull(saved);
        Assert.Equal("Reception", saved.Label);
        Assert.Equal("530-555-4000", saved.Phone);
    }

    // Add and update share one ValidateRequest, but each has to call it. Both paths are exercised
    // so that dropping the call from either is caught: a shared helper is only as good as the
    // call sites, and nothing else in these tests reaches the add path with a blank field.
    [Theory]
    [InlineData("", "530-555-4000", "Location must not be empty.")]
    [InlineData("   ", "530-555-4000", "Location must not be empty.")]
    [InlineData("Reception", "", "Phone Number must not be empty.")]
    [InlineData("Reception", "   ", "Phone Number must not be empty.")]
    public async Task AddFrequentNumber_Throws_ForBlankFields(string label, string phone, string expectedMessage)
    {
        var request = new SVMFrequentNumberRequest { Label = label, Phone = phone };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddFrequentNumber(request, TestContext.Current.CancellationToken));

        Assert.Equal(expectedMessage, ex.Message);
        Assert.Empty(await _service.GetSVMFrequentNumbers(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("", "530-555-4000", "Location must not be empty.")]
    // Whitespace-only rather than empty: the guard is IsNullOrWhiteSpace, and a bare "" would
    // still pass if it were ever weakened to IsNullOrEmpty.
    [InlineData("   ", "530-555-4000", "Location must not be empty.")]
    [InlineData("Reception", "", "Phone Number must not be empty.")]
    [InlineData("Reception", "   ", "Phone Number must not be empty.")]
    public async Task UpdateFrequentNumber_Throws_ForBlankFields(string label, string phone, string expectedMessage)
    {
        SeedNumber(label: "Front Desk", phone: "530-555-1000");
        var request = new SVMFrequentNumberRequest { Label = label, Phone = phone };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateFrequentNumber(1, request, TestContext.Current.CancellationToken));

        Assert.Equal(expectedMessage, ex.Message);
        var unchanged = await FindNumber(1);
        Assert.NotNull(unchanged);
        Assert.Equal("Front Desk", unchanged.Label);
    }

    [Fact]
    public async Task UpdateFrequentNumber_Throws_WhenTheRowWasAlreadyDeleted()
    {
        // A maintainer whose page predates someone else's delete. Editing must not resurrect the
        // row, which the IsActive half of the guard is what prevents - an id-only lookup would
        // find the soft-deleted record and happily write to it.
        SeedNumber(label: "Retired Line", phone: "530-555-3000", isActive: false);
        var request = new SVMFrequentNumberRequest { Label = "Reception", Phone = "530-555-4000" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateFrequentNumber(1, request, TestContext.Current.CancellationToken));

        // Not "not found", which is what this used to say of a row the maintainer can still see.
        Assert.Equal("Frequent number is already deleted.", ex.Message);
        var stillDeleted = await FindNumber(1);
        Assert.NotNull(stillDeleted);
        Assert.False(stillDeleted.IsActive);
        Assert.Equal("Retired Line", stillDeleted.Label);
    }

    [Fact]
    public async Task UpdateFrequentNumber_Throws_WhenNoSuchRowExists()
    {
        SeedNumber(label: "Front Desk", phone: "530-555-1000");
        var request = new SVMFrequentNumberRequest { Label = "Reception", Phone = "530-555-4000" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateFrequentNumber(999, request, TestContext.Current.CancellationToken));

        Assert.Equal("Frequent number not found.", ex.Message);
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
