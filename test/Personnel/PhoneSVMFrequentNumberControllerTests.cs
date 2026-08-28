using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Controller wiring tests for PhoneSVMFrequentNumberController: verifies the
/// InvalidOperationException-to-400 mapping used across the phones endpoints when
/// a maintain action targets a row that doesn't exist (or was already removed).
/// </summary>
public sealed class PhoneSVMFrequentNumberControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneSVMFrequentNumberController _controller;

    public PhoneSVMFrequentNumberControllerTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);

        var userHelper = Substitute.For<IUserHelper>();
        userHelper.GetCurrentUser().Returns(new AaudUser
        {
            ClientId = "ucd.edu",
            MothraId = "caller01",
            LastName = "Caller",
            FirstName = "Test",
            DisplayLastName = "Caller",
            DisplayFirstName = "Test",
            DisplayFullName = "Test Caller",
            IamId = "caller01",
        });

        _controller = new PhoneSVMFrequentNumberController(new PhoneSVMFrequentNumberService(_context, userHelper), Substitute.For<ILogger<PhoneSVMFrequentNumberController>>());
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task UpdateFrequentNumber_ReturnsBadRequest_WhenNotFound()
    {
        var request = new SVMFrequentNumberRequest { Label = "Front Desk", Phone = "530-555-1000" };

        var result = await _controller.UpdateFrequentNumber(999, request, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteFrequentNumber_ReturnsBadRequest_WhenNotFound()
    {
        var result = await _controller.DeleteFrequentNumber(999, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteFrequentNumber_ReturnsOk_AndSoftDeletes_WhenFound()
    {
        _context.SVMFrequentNumber.Add(new SVMFrequentNumber
        {
            NumberId = 1,
            Label = "Pharmacy",
            Phone = "530-555-2000",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.DeleteFrequentNumber(1, TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result);
        var stillExists = await _context.SVMFrequentNumber
            .FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.NotNull(stillExists);
        Assert.False(stillExists.IsActive);
    }

    [Fact]
    public async Task GetFrequentNumbers_ReturnsOnlyActiveNumbers()
    {
        _context.SVMFrequentNumber.AddRange(
            new SVMFrequentNumber { NumberId = 1, Label = "Active Line", Phone = "1", IsActive = true },
            new SVMFrequentNumber { NumberId = 2, Label = "Retired Line", Phone = "2", IsActive = false }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetFrequentNumbers(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var numbers = Assert.IsAssignableFrom<List<SVMFrequentNumberDto>>(okResult.Value);
        Assert.Equal(["Active Line"], numbers.Select(n => n.Label));
    }
}
