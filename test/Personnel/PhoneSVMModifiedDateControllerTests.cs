using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Controller tests for PhoneSVMModifiedDateController, which clients poll to decide whether their
/// cached SVM list is stale. The SVM page renders two independently-maintained datasets - frequent
/// numbers and unit people - behind one freshness date, so the endpoint has to report the later of
/// the two and stay correct when either side has never been modified. Reporting the earlier one
/// would leave a client believing its copy is current.
/// </summary>
public sealed class PhoneSVMModifiedDateControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneSVMModifiedDateController _controller;

    private static readonly DateTime Older = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Local);
    private static readonly DateTime Newer = new(2026, 6, 1, 9, 0, 0, DateTimeKind.Local);

    public PhoneSVMModifiedDateControllerTests()
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

        _controller = new PhoneSVMModifiedDateController(
            new PhoneSVMFrequentNumberService(_context, userHelper),
            new PhoneSVMUnitService(_context, userHelper));

        _context.SVMUnit.Add(new SVMUnit { UnitId = 1, SectionId = 1, Name = "Dean's Office" });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private void AddFrequentNumber(DateTime? modifiedDate)
    {
        _context.SVMFrequentNumber.Add(new SVMFrequentNumber
        {
            NumberId = 1,
            Label = "Front Desk",
            Phone = "530-555-1000",
            IsActive = true,
            ModifiedDate = modifiedDate,
        });
        _context.SaveChanges();
    }

    private void AddUnitPerson(DateTime? modifiedDate)
    {
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
            ModifiedDate = modifiedDate,
        });
        _context.SaveChanges();
    }

    private async Task<DateTime?> GetDate()
    {
        var result = await _controller.GetLastModifiedDate(TestContext.Current.CancellationToken);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return (DateTime?)okResult.Value;
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsNull_WhenNeitherDatasetHasBeenModified()
    {
        Assert.Null(await GetDate());
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsTheUnitPersonDate_WhenNoFrequentNumberHasOne()
    {
        AddUnitPerson(Newer);

        Assert.Equal(Newer, await GetDate());
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsTheFrequentNumberDate_WhenNoUnitPersonHasOne()
    {
        AddFrequentNumber(Newer);

        Assert.Equal(Newer, await GetDate());
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsTheUnitPersonDate_WhenItIsTheLater()
    {
        AddFrequentNumber(Older);
        AddUnitPerson(Newer);

        Assert.Equal(Newer, await GetDate());
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsTheFrequentNumberDate_WhenItIsTheLater()
    {
        AddFrequentNumber(Newer);
        AddUnitPerson(Older);

        Assert.Equal(Newer, await GetDate());
    }
}
