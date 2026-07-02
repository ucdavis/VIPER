using System.Security.Cryptography;
using System.Text;
using Viper.Areas.CMS.Data;

namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// CMS file encryption primitives, byte-for-byte compatible with the legacy ColdFusion CMS
    /// (CF encrypt()/encryptBinary() with algorithm "AES" = AES-128-ECB-PKCS7, key strings UU-encoded).
    /// Both systems share the same files and database during the migration, so the format cannot
    /// change until ColdFusion is decommissioned (see PLAN-CMS.md §12.6 for the GCM migration plan).
    ///
    /// Key model:
    ///  - The master key is a base64 string on line 2 of viperfiles.txt.
    ///  - Each file has its own random AES-128 key. The DB "key" column stores
    ///    UUEncode(AES-ECB(masterKey, base64(fileKey))).
    ///  - File contents are AES-ECB encrypted with the per-file key.
    /// </summary>
    public static class CmsFileCrypto
    {
        /// <summary>
        /// Default location of the master key file for the current environment.
        /// </summary>
        public static string GetDefaultKeyFilePath()
        {
            string settingsFolder = HttpHelper.Environment?.EnvironmentName == "Development"
                ? @"C:\Sites\Settings"
                : @"S:\Settings";
            return Path.Join(settingsFolder, "viperfiles.txt");
        }

        /// <summary>
        /// Read the master key (base64 string) from line 2 of the key file.
        /// </summary>
        public static string ReadMasterKey(string keyFilePath)
        {
            string? masterKey = File.ReadLines(keyFilePath).Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(masterKey))
            {
                throw new InvalidOperationException("CMS master key file is missing the key line.");
            }
            return masterKey.Trim();
        }

        /// <summary>
        /// Generate a new random per-file AES-128 key, returned as base64 (the format CF
        /// generateSecretKey("AES") produced).
        /// </summary>
        public static string GenerateFileKey()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        }

        /// <summary>
        /// Encrypt a per-file key for storage in the files.key column:
        /// UUEncode(AES-ECB(masterKey, UTF8(fileKeyBase64))).
        /// </summary>
        public static string EncryptDbKey(string fileKeyBase64, string masterKeyBase64)
        {
            byte[] cipher = EncryptEcb(Encoding.UTF8.GetBytes(fileKeyBase64), Convert.FromBase64String(masterKeyBase64));
            using MemoryStream input = new(cipher);
            using MemoryStream output = new();
            Codecs.UUEncode(input, output);
            return Encoding.ASCII.GetString(output.ToArray());
        }

        /// <summary>
        /// Decrypt a files.key column value back to the per-file key (base64 string).
        /// Mirrors legacy CF decrypt(key, masterKey, "AES") with default UU encoding.
        /// </summary>
        public static string DecryptDbKey(string dbKey, string masterKeyBase64)
        {
            using MemoryStream input = new(Encoding.ASCII.GetBytes(dbKey));
            using MemoryStream decoded = new();
            Codecs.UUDecode(input, decoded);
            byte[] plain = DecryptEcb(decoded.ToArray(), Convert.FromBase64String(masterKeyBase64));
            return Encoding.UTF8.GetString(plain);
        }

        /// <summary>
        /// Encrypt file contents with a per-file key (base64), matching CF encryptBinary(data, key, "AES").
        /// </summary>
        public static byte[] EncryptBytes(byte[] data, string fileKeyBase64)
        {
            return EncryptEcb(data, Convert.FromBase64String(fileKeyBase64));
        }

        /// <summary>
        /// Decrypt file contents with a per-file key (base64), matching CF decryptBinary(data, key, "AES").
        /// </summary>
        public static byte[] DecryptBytes(byte[] data, string fileKeyBase64)
        {
            return DecryptEcb(data, Convert.FromBase64String(fileKeyBase64));
        }

        private static byte[] EncryptEcb(byte[] data, byte[] key)
        {
            using Aes aes = CreateCfCompatibleAes(key);
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] DecryptEcb(byte[] data, byte[] key)
        {
            using Aes aes = CreateCfCompatibleAes(key);
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static Aes CreateCfCompatibleAes(byte[] key)
        {
            Aes aes = Aes.Create();
            // Settings match ColdFusion's "AES" algorithm defaults; ECB is required for
            // compatibility with existing encrypted files (GCM migration is planned post-CF).
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            return aes;
        }
    }
}
