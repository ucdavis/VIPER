using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using File = Viper.Models.VIPER.File;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsFileImportService
    {
        Task<List<CmsFileImportResult>> ImportFilesAsync(CmsFileImportRequest request, CancellationToken ct = default);

        /// <summary>Dry-run of an import request: validates each path and reports the resulting names without moving anything.</summary>
        Task<List<CmsFileImportPreviewResult>> PreviewImportAsync(CmsFileImportRequest request, CancellationToken ct = default);

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
        private readonly ILogger<CmsFileImportService> _logger;
        private readonly string? _legacyWebroot;

        public CmsFileImportService(VIPERContext context, ICmsFileStorageService storage,
            ICmsFileEncryptionService encryption, ICmsFileAuditService audit, IUserHelper userHelper,
            IConfiguration configuration, ILogger<CmsFileImportService> logger)
        {
            _context = context;
            _storage = storage;
            _encryption = encryption;
            _audit = audit;
            _userHelper = userHelper;
            _logger = logger;
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

        public async Task<List<CmsFileImportPreviewResult>> PreviewImportAsync(CmsFileImportRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_legacyWebroot))
            {
                throw new InvalidOperationException("CMS:LegacyWebrootPath is not configured for this environment.");
            }
            if (!_storage.IsValidFolder(request.Folder))
            {
                throw new ArgumentException("Invalid folder.");
            }

            var results = new List<CmsFileImportPreviewResult>();
            var seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plannedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawPath in request.FilePaths.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()))
            {
                var result = new CmsFileImportPreviewResult { FilePath = rawPath };
                results.Add(result);

                var source = ResolveSource(rawPath);
                if (source.Error != null)
                {
                    result.Message = source.Error;
                    continue;
                }
                if (!seenSources.Add(source.SourcePath))
                {
                    result.Message = "Duplicate of an earlier line.";
                    continue;
                }

                string finalName = _storage.GetAvailableFileName(request.Folder, source.FileName);
                string friendlyName = CmsFileNaming.BuildFriendlyName(request.Folder, finalName);
                if (await _context.Files.AnyAsync(f => f.FriendlyName == friendlyName, ct))
                {
                    result.Message = $"A file with the name {friendlyName} already exists.";
                    continue;
                }

                result.CanImport = true;
                result.FileName = finalName;
                if (!string.Equals(finalName, source.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    result.RenamedFrom = source.FileName;
                }
                result.FriendlyName = friendlyName;
                result.OldUrl = "/" + source.Relative.Replace('\\', '/');
                if (!plannedNames.Add(finalName))
                {
                    result.Message = "Another line uses the same file name; this one will be renamed during import.";
                }
            }
            return results;
        }

        private sealed record SourceResolution(string Relative, string SourcePath, string FileName, string? Error);

        /// <summary>
        /// Resolves a legacy-webroot path and runs the validations shared by preview and
        /// import, so the preview can't promise something the import would reject.
        /// </summary>
        private SourceResolution ResolveSource(string rawPath)
        {
            string relative = rawPath.TrimStart('/', '\\').Replace('/', '\\');
            string sourcePath;
            string fileName;
            try
            {
                sourcePath = Path.GetFullPath(Path.Join(_legacyWebroot, relative));
                fileName = Path.GetFileName(sourcePath);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return new SourceResolution(relative, string.Empty, string.Empty, "Path is not valid.");
            }
            string webroot = Path.GetFullPath(_legacyWebroot! + Path.DirectorySeparatorChar);

            string? error = null;
            if (!sourcePath.StartsWith(webroot, StringComparison.OrdinalIgnoreCase))
            {
                error = "Path is outside the legacy webroot.";
            }
            else if (!System.IO.File.Exists(sourcePath))
            {
                error = "File not found.";
            }
            else if (!CmsFileTypes.IsAllowedFileName(fileName))
            {
                error = "File type is not allowed.";
            }
            return new SourceResolution(relative, sourcePath, fileName, error);
        }

        private async Task<CmsFileImportResult> ImportSingleFileAsync(string rawPath, CmsFileImportRequest request,
            List<string> permissions, CancellationToken ct)
        {
            var result = new CmsFileImportResult { FilePath = rawPath };
            try
            {
                var source = ResolveSource(rawPath);
                if (source.Error != null)
                {
                    result.Message = source.Error;
                    return result;
                }
                string sourcePath = source.SourcePath;

                bool encrypt = request.Encrypt ?? false;
                string? dbKey = encrypt ? _encryption.GenerateKeyForDb() : null;

                // Copy to temp, transform, move into the store; the source is removed only
                // after the import fully succeeds (legacy used move semantics).
                string tempPath = Path.Join(Path.GetTempPath(), "Viper-CMS-Uploads", Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                string finalPath;
                try
                {
                    // Copy inside the try so a partial copy (e.g. disk full) is removed by the finally.
                    System.IO.File.Copy(sourcePath, tempPath);
                    if (dbKey != null)
                    {
                        _encryption.EncryptFileInPlace(tempPath, dbKey);
                    }
                    finalPath = _storage.MoveIntoPlace(tempPath, request.Folder, source.FileName, makeUnique: true);
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }

                string friendlyName = CmsFileNaming.BuildFriendlyName(request.Folder, Path.GetFileName(finalPath));
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
                    OldUrl = "/" + source.Relative.Replace('\\', '/'),
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
                catch (OperationCanceledException)
                {
                    // A cancelled save never persisted the record: drop the pending entity and the
                    // stored copy so the abort leaves nothing orphaned, then let it propagate.
                    _context.ChangeTracker.Clear();
                    _storage.DeleteManagedFile(finalPath);
                    throw;
                }
                catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
                {
                    // Drop the failed entities so they can't poison later saves in this batch,
                    // and remove the stored copy; the untouched source can be re-imported.
                    _context.ChangeTracker.Clear();
                    _storage.DeleteManagedFile(finalPath);
                    result.Message = ex.InnerException?.Message ?? ex.Message;
                    return result;
                }

                try
                {
                    System.IO.File.Delete(sourcePath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The record is saved and the managed copy is in place, so the import
                    // succeeded; only the legacy source could not be removed. Re-importing it
                    // would now fail on the duplicate name, so report success with a note.
                    _logger.LogWarning(ex, "CMS import succeeded but the legacy source could not be removed: {Path}",
                        LogSanitizer.SanitizeString(sourcePath));
                    result.Message = "Imported, but the original file could not be removed from the legacy webroot.";
                }

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
                try
                {
                    // The file is already encrypted on disk; cancelling this save would lose the
                    // key, so it runs to completion even if the request is aborted.
                    await _context.SaveChangesAsync(CancellationToken.None);
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    // Any save failure, whatever the exception type, means the key was not
                    // persisted; the file must be decrypted back or it is unrecoverable.
                    RollBackUnsavedEncryption(file, dbKey, ex, result);
                }
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
