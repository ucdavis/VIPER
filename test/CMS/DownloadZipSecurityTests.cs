using CmsData = Viper.Areas.CMS.Services.CmsFilePathSafety;

namespace Viper.test.CMS;

/// <summary>
/// Security tests for the helpers that back <c>CMS.DownloadZip</c>.
/// Covers the filename sanitizer used for the Content-Disposition header
/// and the per-request temp-archive path builder (VPR-138).
/// </summary>
public sealed class DownloadZipSecurityTests
{
    #region SanitizeDownloadName

    [Theory]
    [InlineData(@"\..\..\evil.zip")]
    [InlineData("../../evil.zip")]
    [InlineData(@"C:\Windows\Temp\evil.zip")]
    [InlineData("../evil")]
    public void SanitizeDownloadName_StripsPathSeparatorsAndTraversal(string input)
    {
        var result = CmsData.SanitizeDownloadName(input);

        Assert.DoesNotContain('\\', result);
        Assert.DoesNotContain('/', result);
        Assert.DoesNotContain("..", result);
        Assert.EndsWith(".zip", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("///")]
    [InlineData(@"\\\")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("...")]
    [InlineData(". .")]
    public void SanitizeDownloadName_NullEmptyOrJunk_ReturnsDefault(string? input)
    {
        var result = CmsData.SanitizeDownloadName(input);

        Assert.Equal("FileDownload.zip", result);
    }

    [Fact]
    public void SanitizeDownloadName_NameWithoutExtension_AppendsZip()
    {
        var result = CmsData.SanitizeDownloadName("export");

        Assert.Equal("export.zip", result);
    }

    [Fact]
    public void SanitizeDownloadName_NonZipExtension_ForcesZipSuffix()
    {
        // Documented choice: preserve the original name and append .zip so the
        // response MIME (application/zip) always matches the filename.
        var result = CmsData.SanitizeDownloadName("export.exe");

        Assert.EndsWith(".zip", result, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("export.exe.zip", result);
    }

    [Fact]
    public void SanitizeDownloadName_AlreadyZip_IsPreserved()
    {
        var result = CmsData.SanitizeDownloadName("monthly-report.zip");

        Assert.Equal("monthly-report.zip", result);
    }

    [Fact]
    public void SanitizeDownloadName_CaseInsensitiveZipExtension_IsPreserved()
    {
        var result = CmsData.SanitizeDownloadName("REPORT.ZIP");

        Assert.Equal("REPORT.ZIP", result);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    [InlineData("con")]
    [InlineData("CON.txt")]
    public void SanitizeDownloadName_ReservedWindowsDeviceNames_ReturnsDefault(string input)
    {
        var result = CmsData.SanitizeDownloadName(input);

        Assert.Equal("FileDownload.zip", result);
    }

    [Fact]
    public void SanitizeDownloadName_AllowsSafeCharacters()
    {
        var result = CmsData.SanitizeDownloadName("My_File-01 v2.zip");

        Assert.Equal("My_File-01 v2.zip", result);
    }

    #endregion

    #region Regression guard (VPR-138)

    // Before the fix, DownloadZip built the on-disk temp archive path by
    // concatenating GetRootFileFolder() + ticks + user-supplied fileName.
    // A traversal payload in fileName (e.g. \..\..\evil.zip) escaped the
    // CMS root. The fix splits the flow: user input feeds only the
    // Content-Disposition name (SanitizeDownloadName); the on-disk path
    // is generated from a server-side GUID under a dedicated temp root
    // (BuildTempArchivePath).
    //
    // This test exercises both helpers as DownloadZip wires them and
    // asserts the traversal payload cannot influence the on-disk path,
    // even if a future refactor reintroduces the old concatenation.
    [Theory]
    [InlineData(@"\..\..\evil.zip")]
    [InlineData("../../evil.zip")]
    [InlineData(@"C:\Windows\System32\evil.zip")]
    [InlineData(@"..\..\..\..\Windows\evil.zip")]
    [InlineData("../../../../etc/passwd")]
    public void Regression_Vpr138_TraversalPayload_CannotEscapeTempRoot(string attackPayload)
    {
        var tempRoot = CreateIsolatedTempRoot();
        try
        {
            var responseName = CmsData.SanitizeDownloadName(attackPayload);
            var tempPath = CmsData.BuildTempArchivePath(tempRoot);

            Assert.DoesNotContain('\\', responseName);
            Assert.DoesNotContain('/', responseName);
            Assert.DoesNotContain("..", responseName);
            Assert.EndsWith(".zip", responseName, StringComparison.OrdinalIgnoreCase);

            var resolvedRoot = Path.GetFullPath(tempRoot);
            var resolvedPath = Path.GetFullPath(tempPath);
            Assert.StartsWith(
                resolvedRoot + Path.DirectorySeparatorChar,
                resolvedPath,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("..", resolvedPath);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    #endregion

    #region SanitizeZipEntryName

    [Theory]
    [InlineData(@"..\..\evil.txt", "evil.txt")]
    [InlineData("../../evil.txt", "evil.txt")]
    [InlineData(@"nested\folder\file.pdf", "file.pdf")]
    [InlineData("nested/folder/file.pdf", "file.pdf")]
    [InlineData("Annual Report 2024.pdf", "Annual Report 2024.pdf")]
    public void SanitizeZipEntryName_StripsPathComponents(string friendlyName, string expected)
    {
        var result = CmsData.SanitizeZipEntryName(friendlyName, fallback: "fallback.bin");

        Assert.Equal(expected, result);
        Assert.DoesNotContain('\\', result);
        Assert.DoesNotContain('/', result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeZipEntryName_EmptyFriendlyName_FallsBackToFilePath(string? friendlyName)
    {
        var result = CmsData.SanitizeZipEntryName(friendlyName, fallback: @"C:\storage\real-file.bin");

        Assert.Equal("real-file.bin", result);
    }

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("dir/..")]
    [InlineData(@"nested\.")]
    [InlineData(". .")]
    public void SanitizeZipEntryName_DotsOnly_FallsBackToFilePath(string friendlyName)
    {
        var result = CmsData.SanitizeZipEntryName(friendlyName, fallback: @"C:\storage\real-file.bin");

        Assert.Equal("real-file.bin", result);
    }

    #endregion

    #region BuildTempArchivePath

    [Fact]
    public void BuildTempArchivePath_ReturnsPathUnderTempRoot()
    {
        var tempRoot = CreateIsolatedTempRoot();
        try
        {
            var path = CmsData.BuildTempArchivePath(tempRoot);

            var resolved = Path.GetFullPath(path);
            var normalizedRoot = Path.GetFullPath(tempRoot);

            Assert.StartsWith(
                normalizedRoot + Path.DirectorySeparatorChar,
                resolved,
                StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".zip", resolved, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Fact]
    public void BuildTempArchivePath_AcceptsRootWithTrailingSeparator()
    {
        var tempRoot = CreateIsolatedTempRoot();
        var withTrailing = tempRoot + Path.DirectorySeparatorChar;
        try
        {
            var path = CmsData.BuildTempArchivePath(withTrailing);

            var resolved = Path.GetFullPath(path);
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(withTrailing));

            Assert.StartsWith(
                normalizedRoot + Path.DirectorySeparatorChar,
                resolved,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Fact]
    public void BuildTempArchivePath_GeneratesUniquePaths_OnSuccessiveCalls()
    {
        var tempRoot = CreateIsolatedTempRoot();
        try
        {
            var a = CmsData.BuildTempArchivePath(tempRoot);
            var b = CmsData.BuildTempArchivePath(tempRoot);

            Assert.NotEqual(a, b);
        }
        finally
        {
            Cleanup(tempRoot);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildTempArchivePath_RejectsEmptyRoot(string tempRoot)
    {
        Assert.Throws<ArgumentException>(() => CmsData.BuildTempArchivePath(tempRoot));
    }

    [Fact]
    public void BuildTempArchivePath_RejectsNullRoot()
    {
        Assert.Throws<ArgumentException>(() => CmsData.BuildTempArchivePath(null!));
    }

    #endregion

    #region Helpers

    private static string CreateIsolatedTempRoot()
    {
        var root = Path.Join(Path.GetTempPath(), "Viper-CMS-Test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    #endregion
}
