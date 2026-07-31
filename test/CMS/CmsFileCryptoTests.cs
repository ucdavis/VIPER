using System.Security.Cryptography;
using System.Text;
using Viper.Areas.CMS.Services;

namespace Viper.test.CMS;

/// <summary>
/// Tests for the CF-compatible CMS file crypto primitives. The DB key format
/// (UUEncode over AES-ECB) and content encryption must round-trip so files written
/// by the new system stay readable by the legacy ColdFusion CMS and vice versa.
/// </summary>
public sealed class CmsFileCryptoTests
{
    private static string NewMasterKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    [Fact]
    public void GenerateFileKey_IsBase64Of128BitKey()
    {
        var key = CmsFileCrypto.GenerateFileKey();

        var bytes = Convert.FromBase64String(key);
        Assert.Equal(16, bytes.Length);
    }

    [Fact]
    public void DbKey_RoundTrips_ThroughEncryptAndDecrypt()
    {
        var masterKey = NewMasterKey();
        var fileKey = CmsFileCrypto.GenerateFileKey();

        var dbKey = CmsFileCrypto.EncryptDbKey(fileKey, masterKey);
        var decrypted = CmsFileCrypto.DecryptDbKey(dbKey, masterKey);

        Assert.Equal(fileKey, decrypted);
        // Stored keys must be printable (UU-encoded) for the varchar column.
        Assert.All(dbKey, c => Assert.True(c < 127));
    }

    [Fact]
    public void DbKey_DiffersPerCall_EvenForSameFileKey()
    {
        // Same plaintext under different master keys must never produce the same stored key.
        var fileKey = CmsFileCrypto.GenerateFileKey();

        var dbKey1 = CmsFileCrypto.EncryptDbKey(fileKey, NewMasterKey());
        var dbKey2 = CmsFileCrypto.EncryptDbKey(fileKey, NewMasterKey());

        Assert.NotEqual(dbKey1, dbKey2);
    }

    [Fact]
    public void FileContents_RoundTrip_ThroughEncryptAndDecrypt()
    {
        var fileKey = CmsFileCrypto.GenerateFileKey();
        var contents = Encoding.UTF8.GetBytes("PDF-1.7 pretend file contents éü with some length to cross a block boundary.");

        var encrypted = CmsFileCrypto.EncryptBytes(contents, fileKey);
        var decrypted = CmsFileCrypto.DecryptBytes(encrypted, fileKey);

        Assert.NotEqual(contents, encrypted);
        Assert.Equal(contents, decrypted);
    }

    [Fact]
    public void FullFlow_DbKeyPlusContents_MatchesLegacyDecryptPath()
    {
        // Mirrors CMS.DecryptFile: db key -> per-file key -> ECB decrypt of contents.
        var masterKey = NewMasterKey();
        var fileKey = CmsFileCrypto.GenerateFileKey();
        var dbKey = CmsFileCrypto.EncryptDbKey(fileKey, masterKey);
        var contents = RandomNumberGenerator.GetBytes(1024);

        var encrypted = CmsFileCrypto.EncryptBytes(contents, fileKey);
        var decrypted = CmsFileCrypto.DecryptBytes(encrypted, CmsFileCrypto.DecryptDbKey(dbKey, masterKey));

        Assert.Equal(contents, decrypted);
    }

    [Fact]
    public void ReadMasterKey_ReadsSecondLine()
    {
        var path = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            File.WriteAllLines(path, new[] { "first line is ignored", "  the-master-key  ", "third" });

            Assert.Equal("the-master-key", CmsFileCrypto.ReadMasterKey(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadMasterKey_MissingKeyLine_Throws()
    {
        var path = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            File.WriteAllLines(path, new[] { "only one line" });

            Assert.Throws<InvalidOperationException>(() => CmsFileCrypto.ReadMasterKey(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
