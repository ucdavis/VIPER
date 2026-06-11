using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Classes.SQLContext;
using File = Viper.Models.VIPER.File;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsFileImportService
    {
        Task<List<CmsFileImportResult>> ImportFilesAsync(CmsFileImportRequest request, CancellationToken ct = default);

        Task<List<CmsBulkEncryptResult>> BulkEncryptAsync(List<Guid> fileGuids, CancellationToken ct = default);
    }

    /// <summary>
    /// Bulk file tools mirroring the legacy CMS: import loose files from the legacy VIPER
    /// webroot into the managed store (recording the original path as oldURL so legacy links
    /// keep working via the oldURL lookup), and bulk-encrypt existing unencrypted files.
    /// </summary>
    public class CmsFileImportService : ICmsFileImportService
    {
        private readonly VIPERContext _context;
        private readonly ICmsFileStorageService _storage;
        private readonly ICmsFileEncryptionService _encryption;
        private readonly ICmsFileAuditService _audit;
        private readonly IUserHelper _userHelper;
        private readonly string? _legacyWebroot;

        public CmsFileImportService(VIPERContext context, ICmsFileStorageService storage,
            ICmsFileEncryptionService encryption, ICmsFileAuditService audit, IUserHelper userHelper,
            IConfiguration configuration)
        {
            _context = context;
            _storage = storage;
            _encryption = encryption;
            _audit = audit;
            _userHelper = userHelper;
            _legacyWebroot = configuration["CMS:LegacyWebrootPath"]
                ?? (HttpHelper.Environment?.EnvironmentName == "Development" ? @"C:\Sites\https\VIPER" : null);
        }

        public async Task<List<CmsFileImportResult>> ImportFilesAsync(CmsFileImportRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_legacyWebroot))
            {
                throw new InvalidOperationException("CMS:LegacyWebrootPath is not configured for this environment.");
            }
            if (!_storage.IsValidFolder(request.Folder))
            {
                throw new ArgumentException("Invalid folder.");
            }

            var permissions = request.Permissions
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();
            if (request.UseDefaultPermission == true)
            {
                string topFolder = request.Folder.Split(['\\', '/'], StringSplitOptions.None)[0];
                permissions.Add("SVMSecure." + topFolder);
            }
            permissions = permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var results = new List<CmsFileImportResult>();
            foreach (var rawPath in request.FilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                results.Add(await ImportSingleFileAsync(rawPath.Trim(), request, permissions, ct));
            }
            return results;
        }

        private async Task<CmsFileImportResult> ImportSingleFileAsync(string rawPath, CmsFileImportRequest request,
            List<string> permissions, CancellationToken ct)
        {
            var result = new CmsFileImportResult { FilePath = rawPath };
            try
            {
                string relative = rawPath.TrimStart('/', '\\').Replace('/', '\\');
                string sourcePath = Path.GetFullPath(Path.Join(_legacyWebroot, relative));
                string webroot = Path.GetFullPath(_legacyWebroot! + Path.DirectorySeparatorChar);
                if (!sourcePath.StartsWith(webroot, StringComparison.OrdinalIgnoreCase))
                {
                    result.Message = "Path is outside the legacy webroot.";
                    return result;
                }
                if (!System.IO.File.Exists(sourcePath))
                {
                    result.Message = "File not found.";
                    return result;
                }
                string fileName = Path.GetFileName(sourcePath);
                if (!CmsFileTypes.IsAllowedFileName(fileName))
                {
                    result.Message = "File type is not allowed.";
                    return result;
                }

                bool encrypt = request.Encrypt ?? false;
                string? dbKey = encrypt ? _encryption.GenerateKeyForDb() : null;

                // Copy to temp, transform, move into the store; the source is removed only
                // after the import fully succeeds (legacy used move semantics).
                string tempPath = Path.Join(Path.GetTempPath(), "Viper-CMS-Uploads", Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
                System.IO.File.Copy(sourcePath, tempPath);

                string finalPath;
                try
                {
                    if (dbKey != null)
                    {
                        _encryption.EncryptFileInPlace(tempPath, dbKey);
                    }
                    finalPath = _storage.MoveIntoPlace(tempPath, request.Folder, fileName, makeUnique: true);
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }

                string friendlyName = request.Folder.Replace('\\', '-').Replace('/', '-') + "-" + Path.GetFileName(finalPath);
                if (await _context.Files.AnyAsync(f => f.FriendlyName == friendlyName, ct))
                {
                    _storage.DeleteManagedFile(finalPath);
                    result.Message = $"A file with the name {friendlyName} already exists.";
                    return result;
                }

                var entity = new File
                {
                    FileGuid = Guid.NewGuid(),
                    FilePath = finalPath,
                    Folder = request.Folder,
                    FriendlyName = friendlyName,
                    Encrypted = encrypt,
                    Key = dbKey,
                    Description = "Automatically imported from Viper",
                    AllowPublicAccess = request.AllowPublicAccess ?? false,
                    OldUrl = "/" + relative.Replace('\\', '/'),
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = CurrentLoginId()
                };
                foreach (var permission in permissions)
                {
                    entity.FileToPermissions.Add(new Viper.Models.VIPER.FileToPermission { FileGuid = entity.FileGuid, Permission = permission });
                }

                _context.Files.Add(entity);
                _audit.Audit(entity, CmsFileAuditActions.ImportFile, $"Imported from {entity.OldUrl}");
                try
                {
                    await _context.SaveChangesAsync(ct);
                }
                catch (Exception ex) when (ex is DbUpdateException or SqlException)
                {
                    // Drop the failed entities so they can't poison later saves in this batch,
                    // and remove the stored copy; the untouched source can be re-imported.
                    _context.ChangeTracker.Clear();
                    _storage.DeleteManagedFile(finalPath);
                    result.Message = ex.InnerException?.Message ?? ex.Message;
                    return result;
                }

                System.IO.File.Delete(sourcePath);

                result.Success = true;
                result.FileGuid = entity.FileGuid;
                result.FriendlyName = friendlyName;
                return result;
            }
            catch (IOException ex)
            {
                result.Message = ex.Message;
                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Message = ex.Message;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                result.Message = ex.Message;
                return result;
            }
            catch (ArgumentException ex)
            {
                result.Message = ex.Message;
                return result;
            }
        }

        public async Task<List<CmsBulkEncryptResult>> BulkEncryptAsync(List<Guid> fileGuids, CancellationToken ct = default)
        {
            var results = new List<CmsBulkEncryptResult>();
            foreach (var fileGuid in fileGuids.Distinct())
            {
                results.Add(await EncryptSingleFileAsync(fileGuid, ct));
            }
            return results;
        }

        private async Task<CmsBulkEncryptResult> EncryptSingleFileAsync(Guid fileGuid, CancellationToken ct)
        {
            var result = new CmsBulkEncryptResult { FileGuid = fileGuid };
            var file = await _context.Files.FirstOrDefaultAsync(f => f.FileGuid == fileGuid, ct);
            if (file == null)
            {
                result.Message = "File not found.";
                return result;
            }
            result.FriendlyName = file.FriendlyName;
            if (file.Encrypted)
            {
                result.Message = "Already encrypted.";
                return result;
            }
            if (!_storage.ManagedFileExists(file.FilePath))
            {
                result.Message = "File is missing on disk.";
                return result;
            }

            string? dbKey = null;
            try
            {
                dbKey = _encryption.GenerateKeyForDb();
                _encryption.EncryptFileInPlace(file.FilePath, dbKey);
                file.Key = dbKey;
                file.Encrypted = true;
                file.ModifiedOn = DateTime.Now;
                file.ModifiedBy = CurrentLoginId();
                _audit.Audit(file, CmsFileAuditActions.EditFile, "Encrypted file (bulk)");
                // The file is already encrypted on disk; cancelling this save would lose the
                // key, so it runs to completion even if the request is aborted.
                await _context.SaveChangesAsync(CancellationToken.None);
                result.Success = true;
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException)
            {
                RollBackUnsavedEncryption(file, dbKey!, ex, result);
            }
            catch (IOException ex)
            {
                result.Message = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Message = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// A failed save means the key was never persisted, so decrypt the file back to plain
        /// text and drop the pending changes so they can't poison later saves in the batch.
        /// </summary>
        private void RollBackUnsavedEncryption(File file, string dbKey, Exception saveEx, CmsBulkEncryptResult result)
        {
            _context.ChangeTracker.Clear();
            try
            {
                _encryption.DecryptFileInPlace(file.FilePath, dbKey);
                result.Message = saveEx.InnerException?.Message ?? saveEx.Message;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                result.Message = $"{saveEx.Message} The file could not be restored ({ex.Message}); "
                    + "it is encrypted on disk but the key was not saved. Restore it from backup.";
            }
        }

        private string CurrentLoginId()
        {
            return _userHelper.GetCurrentUser()?.LoginId ?? "unknown";
        }
    }
}
