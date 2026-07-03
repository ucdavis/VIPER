using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Controllers;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.test.CMS;

/// <summary>
/// Controller wiring tests for CMSOptionsController: the person search's minimum-length guard,
/// 25-result cap, name/id matching, and last-name/first-name ordering, plus the instance-scoped
/// permission list used by CMS tagging forms.
/// </summary>
public sealed class CMSOptionsControllerTests : IDisposable
{
    private readonly RAPSContext _rapsContext;
    private readonly AAUDContext _aaudContext;
    private readonly CMSOptionsController _controller;

    public CMSOptionsControllerTests()
    {
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        _aaudContext = new AAUDContext(new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase("AAUD_" + Guid.NewGuid()).Options);

        _controller = new CMSOptionsController(_rapsContext, _aaudContext);
    }

    public void Dispose()
    {
        _rapsContext.Dispose();
        _aaudContext.Dispose();
    }

    private static AaudUser MakeUser(int id, string lastName, string firstName, string? loginId = null,
        string? mailId = null, string? iamId = "iam", int current = 1) => new()
        {
            AaudUserId = id,
            ClientId = "test-client",
            MothraId = "m" + id,
            LoginId = loginId,
            MailId = mailId,
            IamId = iamId,
            Current = current,
            LastName = lastName,
            FirstName = firstName,
            DisplayLastName = lastName,
            DisplayFirstName = firstName,
            DisplayFullName = lastName + ", " + firstName
        };

    #region SearchPeople

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public async Task SearchPeople_ReturnsEmpty_WhenSearchBelowMinimumLength(string? search)
    {
        _aaudContext.AaudUsers.Add(MakeUser(1, "Smith", "Amy"));
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.SearchPeople(search!, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task SearchPeople_CapsResultsAt25_WhenMoreThanLimitMatch()
    {
        for (var i = 0; i < 30; i++)
        {
            _aaudContext.AaudUsers.Add(MakeUser(i + 1, "Smith", $"Test{i:D2}"));
        }
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.SearchPeople("Smith", TestContext.Current.CancellationToken);

        Assert.Equal(25, result.Value!.Count);
    }

    [Fact]
    public async Task SearchPeople_OrdersByDisplayLastName_ThenDisplayFirstName()
    {
        _aaudContext.AaudUsers.Add(MakeUser(1, "Zeta", "Amy", mailId: "orderx1"));
        _aaudContext.AaudUsers.Add(MakeUser(2, "Alpha", "Bob", mailId: "orderx2"));
        _aaudContext.AaudUsers.Add(MakeUser(3, "Mno", "Cara", mailId: "orderx3"));
        _aaudContext.AaudUsers.Add(MakeUser(4, "Alpha", "Amy", mailId: "orderx4"));
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.SearchPeople("orderx", TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "Alpha, Amy", "Alpha, Bob", "Mno, Cara", "Zeta, Amy" },
            result.Value!.Select(p => p.Name));
    }

    [Fact]
    public async Task SearchPeople_ExcludesNonCurrentAndMissingIamId()
    {
        _aaudContext.AaudUsers.Add(MakeUser(1, "Smith", "Amy", current: 0));
        _aaudContext.AaudUsers.Add(MakeUser(2, "Smith", "Bob", iamId: null));
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.SearchPeople("Smith", TestContext.Current.CancellationToken);

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task SearchPeople_MatchesOnLoginIdAndMailId()
    {
        _aaudContext.AaudUsers.Add(MakeUser(1, "Doe", "Jane", loginId: "jdoe123", mailId: "jdoe"));
        await _aaudContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var byLogin = await _controller.SearchPeople("jdoe123", TestContext.Current.CancellationToken);
        var byMail = await _controller.SearchPeople("jdoe", TestContext.Current.CancellationToken);

        Assert.Single(byLogin.Value!);
        Assert.Single(byMail.Value!);
    }

    #endregion

    #region GetPermissions

    [Fact]
    public async Task GetPermissions_ReturnsInstancePermissions_SortedAlphabetically_ExcludingVmacsAndViperForms()
    {
        _rapsContext.TblPermissions.AddRange(
            new TblPermission { PermissionId = 1, Permission = "SVMSecure.CMS.Zeta" },
            new TblPermission { PermissionId = 2, Permission = "SVMSecure.CMS.Alpha" },
            new TblPermission { PermissionId = 3, Permission = "VMACS.Admin" },
            new TblPermission { PermissionId = 4, Permission = "VIPERForms.Admin" });
        await _rapsContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetPermissions(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "SVMSecure.CMS.Alpha", "SVMSecure.CMS.Zeta" }, result.Value);
    }

    #endregion
}
