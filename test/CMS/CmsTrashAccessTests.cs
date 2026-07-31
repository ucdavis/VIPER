using Viper.Areas.CMS.Constants;

namespace Viper.test.CMS;

/// <summary>
/// Tests for the trash-visibility rule behind the file download handler.
///
/// A soft-deleted CMS file keeps its bytes on disk for the whole 30-day retention window, so this
/// predicate is the only thing that stops a "deleted" file from staying downloadable at its old
/// URL. Legacy VIPER 1 had no such check at all (cms/CFC/FileDB.cfc get() never filtered
/// deletedOn), which is the bug these tests pin closed.
///
/// The rule intentionally matches CMSFilesController.OwnerRestriction, which scopes the trash
/// listing: admins see everything, other file managers only what they deleted themselves.
/// </summary>
public sealed class CmsTrashAccessTests
{
    private const string Owner = "asmith";

    [Fact]
    public void CanAccessTrashed_AllowsAdmin_RegardlessOfWhoDeletedIt()
    {
        Assert.True(CmsTrash.CanAccessTrashed(isAdmin: true, hasAllFiles: false,
            deletedBy: "someone-else", loginId: Owner));
    }

    [Fact]
    public void CanAccessTrashed_AllowsFileManager_ForTheirOwnDeletion()
    {
        Assert.True(CmsTrash.CanAccessTrashed(isAdmin: false, hasAllFiles: true,
            deletedBy: Owner, loginId: Owner));
    }

    [Fact]
    public void CanAccessTrashed_IsCaseInsensitiveOnLoginId()
    {
        // RAPS login ids arrive with inconsistent casing; a case mismatch must not lock an owner
        // out of a file they can already see listed in their own trash.
        Assert.True(CmsTrash.CanAccessTrashed(isAdmin: false, hasAllFiles: true,
            deletedBy: "ASmith", loginId: "asmith"));
    }

    [Fact]
    public void CanAccessTrashed_DeniesFileManager_ForSomeoneElsesDeletion()
    {
        Assert.False(CmsTrash.CanAccessTrashed(isAdmin: false, hasAllFiles: true,
            deletedBy: "bjones", loginId: Owner));
    }

    [Fact]
    public void CanAccessTrashed_DeniesUserWithoutFilePermissions_EvenForTheirOwnDeletion()
    {
        // Losing AllFiles removes the trash UI, so it must remove the download with it.
        Assert.False(CmsTrash.CanAccessTrashed(isAdmin: false, hasAllFiles: false,
            deletedBy: Owner, loginId: Owner));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CanAccessTrashed_DeniesAnonymousRequest(string? loginId)
    {
        // The main regression: an anonymous request could download a trashed file at its old URL.
        Assert.False(CmsTrash.CanAccessTrashed(isAdmin: false, hasAllFiles: true,
            deletedBy: Owner, loginId: loginId));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void CanAccessTrashed_DeniesWhenBothOwnerAndLoginAreBlank(string? deletedBy, string? loginId)
    {
        // Two blanks must not compare equal into an accidental grant.
        Assert.False(CmsTrash.CanAccessTrashed(isAdmin: false, hasAllFiles: true,
            deletedBy: deletedBy, loginId: loginId));
    }

    [Fact]
    public void CanAccessTrashed_DeniesWhenOwnerIsUnknown()
    {
        // A pre-migration row with no ModifiedBy has no provable owner, so only admins get it.
        Assert.False(CmsTrash.CanAccessTrashed(isAdmin: false, hasAllFiles: true,
            deletedBy: null, loginId: Owner));
        Assert.True(CmsTrash.CanAccessTrashed(isAdmin: true, hasAllFiles: true,
            deletedBy: null, loginId: Owner));
    }
}
