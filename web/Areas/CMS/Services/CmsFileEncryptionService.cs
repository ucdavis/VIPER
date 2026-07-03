namespace Viper.Areas.CMS.Services
{
    public interface ICmsFileEncryptionService
    {
        /// <summary>
        /// Generate a new per-file key, returned in the encrypted form stored in files.key.
        /// </summary>
        string GenerateKeyForDb();

        byte[] Encrypt(byte[] data, string dbKey);

        byte[] Decrypt(byte[] data, string dbKey);

        void EncryptFileInPlace(string filePath, string dbKey);

        void DecryptFileInPlace(string filePath, string dbKey);
    }

    /// <summary>
    /// DI wrapper around <see cref="CmsFileCrypto"/> that resolves and caches the master key.
    /// Registered by the Scrutor convention scan of Viper.Areas.CMS.Services.
    /// </summary>
    public class CmsFileEncryptionService : ICmsFileEncryptionService
    {
        private readonly Lazy<string> _masterKey;

        public CmsFileEncryptionService(IConfiguration configuration)
        {
            string keyFilePath = configuration["CMS:EncryptionKeyFile"] ?? CmsFileCrypto.GetDefaultKeyFilePath();
            _masterKey = new Lazy<string>(() => CmsFileCrypto.ReadMasterKey(keyFilePath));
        }

        public string GenerateKeyForDb()
        {
            return CmsFileCrypto.EncryptDbKey(CmsFileCrypto.GenerateFileKey(), _masterKey.Value);
        }

        public byte[] Encrypt(byte[] data, string dbKey)
        {
            return CmsFileCrypto.EncryptBytes(data, CmsFileCrypto.DecryptDbKey(dbKey, _masterKey.Value));
        }

        public byte[] Decrypt(byte[] data, string dbKey)
        {
            return CmsFileCrypto.DecryptBytes(data, CmsFileCrypto.DecryptDbKey(dbKey, _masterKey.Value));
        }

        public void EncryptFileInPlace(string filePath, string dbKey)
        {
            ReplaceFileContents(filePath, contents => Encrypt(contents, dbKey));
        }

        public void DecryptFileInPlace(string filePath, string dbKey)
        {
            ReplaceFileContents(filePath, contents => Decrypt(contents, dbKey));
        }

        /// <summary>
        /// Rewrite a file via a temp file in the same directory so an interrupted write
        /// can't leave the target truncated or half-transformed.
        /// </summary>
        private static void ReplaceFileContents(string filePath, Func<byte[], byte[]> transform)
        {
            byte[] contents = File.ReadAllBytes(filePath);
            string tempPath = filePath + ".tmp";
            try
            {
                File.WriteAllBytes(tempPath, transform(contents));
                File.Move(tempPath, filePath, overwrite: true);
            }
            finally
            {
                // Any failure path must remove the temp copy - after a decrypt it holds
                // plaintext, and a stray .tmp would otherwise linger in the managed store.
                if (File.Exists(tempPath))
                {
                    CleanUpTempFile(tempPath);
                }
            }
        }

        private static void CleanUpTempFile(string tempPath)
        {
            try
            {
                File.Delete(tempPath);
            }
            catch (IOException)
            {
                // Best effort; the orphaned .tmp file is harmless and the original error matters more.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort; see above.
            }
        }
    }
}
