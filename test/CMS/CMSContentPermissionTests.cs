using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.CMS.Constants;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using Viper.Models.VIPER;
using Viper.Services;
using DataCms = Viper.Areas.CMS.Data.CMS;

namespace Viper.test.CMS;

/// <summary>
/// Tests for Data.CMS.GetContentBlocksAllowed, the public-display permission gate behind the
/// CMS content endpoint (content/fn/{friendlyName}). A public block is served to anyone; a
/// restricted block is withheld unless the viewer is a CMS admin, the block has no permissions
/// and the viewer is logged in, or the viewer holds one of the block's permissions. Permission
/// scenarios are driven by mocking IUserHelper.
/// </summary>
public sealed class CMSContentPermissionTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly DataCms _cms;

    public CMSContentPermissionTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        var sanitizer = Substitute.For<IHtmlSanitizerService>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(c => c.ArgAt<string>(0));
        _userHelper = Substitute.For<IUserHelper>();

        _cms = new DataCms(_context, _rapsContext, sanitizer) { UserHelper = _userHelper };
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    private async Task<ContentBlock> SeedBlockAsync(bool allowPublic, params string[] permissions)
    {
        var block = new ContentBlock
        {
            Content = "<p>secret</p>",
            Title = "Block",
            System = "Viper",
            FriendlyName = "block-" + Guid.NewGuid().ToString("N")[..8],
            AllowPublicAccess = allowPublic,
            ModifiedOn = DateTime.Now,
            ModifiedBy = "author"
        };
        foreach (var permission in permissions)
        {
            block.ContentBlockToPermissions.Add(new ContentBlockToPermission { Permission = permission });
        }
        _context.ContentBlocks.Add(block);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return block;
    }

    private static AaudUser User() => new() { AaudUserId = 1, LoginId = "viewer", MothraId = "m1" };

    private void GrantPermissions(AaudUser user, params string[] permissions)
    {
        _userHelper.GetAllPermissions(_rapsContext, user)
            .Returns(permissions.Select(p => new TblPermission { Permission = p }).ToList());
    }

    [Fact]
    public async Task PublicBlock_ReturnedToAnonymousUser()
    {
        var block = await SeedBlockAsync(allowPublic: true);
        _userHelper.GetCurrentUser().ReturnsNull();

        var result = _cms.GetContentBlocksAllowed(null, block.FriendlyName, null, null, null, null, null, 1)?.ToList();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(block.ContentBlockId, result[0].ContentBlockId);
    }

    [Fact]
    public async Task RestrictedBlock_WithheldFromUnprivilegedUser()
    {
        var block = await SeedBlockAsync(allowPublic: false, "SVMSecure.CATS");
        var user = User();
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(false);
        _userHelper.HasPermission(_rapsContext, user, "SVMSecure").Returns(true);
        // User holds an unrelated permission, not the block's required one.
        GrantPermissions(user, "SVMSecure.Other");

        var result = _cms.GetContentBlocksAllowed(null, block.FriendlyName, null, null, null, null, null, 1)?.ToList();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RestrictedBlock_ReturnedWhenUserHoldsBlockPermission()
    {
        var block = await SeedBlockAsync(allowPublic: false, "SVMSecure.CATS");
        var user = User();
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(false);
        GrantPermissions(user, "SVMSecure.CATS");

        var result = _cms.GetContentBlocksAllowed(null, block.FriendlyName, null, null, null, null, null, 1)?.ToList();

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task RestrictedBlock_PermissionMatchIsCaseInsensitive()
    {
        var block = await SeedBlockAsync(allowPublic: false, "SVMSecure.CATS");
        var user = User();
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(false);
        GrantPermissions(user, "svmsecure.cats");

        var result = _cms.GetContentBlocksAllowed(null, block.FriendlyName, null, null, null, null, null, 1)?.ToList();

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task CmsAdmin_SeesRestrictedBlock()
    {
        var block = await SeedBlockAsync(allowPublic: false, "SVMSecure.CATS");
        var user = User();
        _userHelper.GetCurrentUser().Returns(user);
        // CMS admin override short-circuits before any block-permission match.
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(true);

        var result = _cms.GetContentBlocksAllowed(null, block.FriendlyName, null, null, null, null, null, 1)?.ToList();

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task NoPermissionBlock_ReturnedToAnyLoggedInUser()
    {
        // A block with no permissions is visible to any authenticated VIPER user (has "SVMSecure").
        var block = await SeedBlockAsync(allowPublic: false);
        var user = User();
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks).Returns(false);
        _userHelper.HasPermission(_rapsContext, user, "SVMSecure").Returns(true);

        var result = _cms.GetContentBlocksAllowed(null, block.FriendlyName, null, null, null, null, null, 1)?.ToList();

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task RestrictedBlock_WithheldFromAnonymousUser()
    {
        var block = await SeedBlockAsync(allowPublic: false, "SVMSecure.CATS");
        _userHelper.GetCurrentUser().ReturnsNull();

        var result = _cms.GetContentBlocksAllowed(null, block.FriendlyName, null, null, null, null, null, 1)?.ToList();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
