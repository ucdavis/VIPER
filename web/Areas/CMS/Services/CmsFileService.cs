using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using File = Viper.Models.VIPER.File;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsFileService
    {
        Task<(List<CmsFileDto> Files, int Total)> GetFilesAsync(string? folder, string status, string? search,
            bool? encrypted, bool? isPublic, int page, int perPage, string? sortBy, bool descending,
            string? restrictDeletedToOwner = null, CancellationToken ct = default);

        Task<List<CmsFolderCountDto>> GetFolderCountsAsync(string status, bool? encrypted, bool? isPublic,
            string? restrictDeletedToOwner = null, CancellationToken ct = default);

        Task<CmsFileNameCheckDto> CheckNameAsync(string folder, string fileName, CancellationToken ct = default);

        Task<CmsFileDto?> GetFileAsync(Guid fileGuid, CancellationToken ct = default);

        Task<CmsFileDto> CreateFileAsync(CmsFileCreateRequest request, IFormFile file, CancellationToken ct = default);

        Task<CmsFileDto?> UpdateFileAsync(Guid fileGuid, CmsFileUpdateRequest request, IFormFile? file, CancellationToken ct = default);

        Task<bool> SoftDeleteFileAsync(Guid fileGuid, CancellationToken ct = default);

        Task<bool> RestoreFileAsync(Guid fileGuid, CancellationToken ct = default);

        Task<bool> PermanentlyDeleteFileAsync(Guid fileGuid, CancellationToken ct = default);

        /// <summary>
        /// Permanently deletes trashed files whose DeletedOn is older than <paramref name="retentionDays"/>.
        /// Used by the trash-purge scheduled job. Returns the number of files purged.
        /// </summary>
        Task<int> PurgeDeletedFilesAsync(int retentionDays, CancellationToken ct = default);
    }

    /// <summary>
    /// Management operations for CMS files (the admin side; downloads are served by
    /// CMSController/Data.CMS). Behavior mirrors the legacy ColdFusion Files.cfc:
    /// friendly names are folder-filename with backslashes dashed, permission and person
    /// lists are replaced as deltas, and every operation writes a fileAudit row.
    /// </summary>
    public class CmsFileService : ICmsFileService
    {
        private readonly VIPERContext _context;
        private readonly AAUDContext _aaudContext;
        private readonly ICmsFileStorageService _storage;
        private readonly ICmsFileEncryptionService _encryption;
        private readonly ICmsFileAuditService _audit;
        private readonly IUserHelper _userHelper;
        private readonly ILogger<CmsFileService> _logger;

        public CmsFileService(VIPERContext context, AAUDContext aaudContext, ICmsFileStorageService storage,
            ICmsFileEncryptionService encryption, ICmsFileAuditService audit, IUserHelper userHelper,
            ILogger<CmsFileService> logger)
        {
            _context = context;
            _aaudContext = aaudContext;
            _storage = storage;
            _encryption = encryption;
            _audit = audit;
            _userHelper = userHelper;
            _logger = logger;
        }

        public async Task<(List<CmsFileDto> Files, int Total)> GetFilesAsync(string? folder, string status, string? search,
            bool? encrypted, bool? isPublic, int page, int perPage, string? sortBy, bool descending,
            string? restrictDeletedToOwner = null, CancellationToken ct = default)
        {
            var query = _context.Files
                .AsNoTracking()
                .Include(f => f.FileToPermissions)
                .Include(f => f.FileToPeople)
                .AsSplitQuery()
                .TagWith("CmsFileService.GetFiles")
                .AsQueryable();

            if (!string.IsNullOrEmpty(folder))
            {
                // Folder may have subfolders stored as "folder\sub"; match the top-level folder.
                var folderPrefix = folder + @"\";
                query = query.Where(f => f.Folder == folder || (f.Folder != null && f.Folder.StartsWith(folderPrefix)));
            }

            query = ApplyStatusFilter(query, status, restrictDeletedToOwner);

            if (encrypted != null)
            {
                query = query.Where(f => f.Encrypted == encrypted);
            }

            if (isPublic != null)
            {
                query = query.Where(f => f.AllowPublicAccess == isPublic);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f => f.FriendlyName.Contains(search)
                    || f.Description.Contains(search)
                    || (f.OldUrl != null && f.OldUrl.Contains(search))
                    || f.FilePath.Contains(search));
            }

            int total = await query.CountAsync(ct);

            query = (sortBy?.ToLowerInvariant(), descending) switch
            {
                ("folder", false) => query.OrderBy(f => f.Folder).ThenBy(f => f.FriendlyName),
                ("folder", true) => query.OrderByDescending(f => f.Folder).ThenBy(f => f.FriendlyName),
                ("modifiedon", false) => query.OrderBy(f => f.ModifiedOn),
                ("modifiedon", true) => query.OrderByDescending(f => f.ModifiedOn),
                ("oldurl", false) => query.OrderBy(f => f.OldUrl),
                ("oldurl", true) => query.OrderByDescending(f => f.OldUrl),
                (_, true) => query.OrderByDescending(f => f.FriendlyName),
                _ => query.OrderBy(f => f.FriendlyName)
            };

            var files = await query
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync(ct);

            var names = await GetNamesByIamIdAsync(files.SelectMany(f => f.FileToPeople.Select(p => p.IamId)), ct);
            return (files.Select(f => CmsFileMapper.ToCmsFileDto(f, names)).ToList(), total);
        }

        public async Task<List<CmsFolderCountDto>> GetFolderCountsAsync(string status, bool? encrypted, bool? isPublic,
            string? restrictDeletedToOwner = null, CancellationToken ct = default)
        {
            var query = _context.Files
                .AsNoTracking()
                .TagWith("CmsFileService.GetFolderCounts")
                .AsQueryable();

            query = ApplyStatusFilter(query, status, restrictDeletedToOwner);

            if (encrypted != null)
            {
                query = query.Where(f => f.Encrypted == encrypted);
            }

            if (isPublic != null)
            {
                query = query.Where(f => f.AllowPublicAccess == isPublic);
            }

            var byFolder = await query
                .GroupBy(f => f.Folder)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct);

            // Folders may have subfolders stored as "folder\sub"; roll counts up to the
            // top-level folder so they match the list's folder filter (GetFilesAsync).
            return byFolder
                .GroupBy(x => (x.Key ?? string.Empty).Split(['\\', '/'], StringSplitOptions.None)[0], StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .Select(g => new CmsFolderCountDto { Folder = g.Key, Count = g.Sum(x => x.Count) })
                .OrderBy(c => c.Folder, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // restrictDeletedToOwner scopes deleted files to the login that deleted them (ModifiedBy),
        // so non-admins only see files they trashed. Admins pass null and see the whole trash.
        private static IQueryable<File> ApplyStatusFilter(IQueryable<File> query, string? status,
            string? restrictDeletedToOwner = null)
        {
            return status?.ToLowerInvariant() switch
            {
                "active" => query.Where(f => f.DeletedOn == null),
                "deleted" => restrictDeletedToOwner == null
                    ? query.Where(f => f.DeletedOn != null)
                    : query.Where(f => f.DeletedOn != null && f.ModifiedBy == restrictDeletedToOwner),
                _ => restrictDeletedToOwner == null
                    ? query
                    : query.Where(f => f.DeletedOn == null || f.ModifiedBy == restrictDeletedToOwner)
            };
        }

        public async Task<CmsFileDto?> GetFileAsync(Guid fileGuid, CancellationToken ct = default)
        {
            var file = await LoadFileAsync(fileGuid, tracking: false, ct);
            if (file == null)
            {
                return null;
            }
            var names = await GetNamesByIamIdAsync(file.FileToPeople.Select(p => p.IamId), ct);
            return CmsFileMapper.ToCmsFileDto(file, names);
        }

        public async Task<CmsFileNameCheckDto> CheckNameAsync(string folder, string fileName, CancellationToken ct = default)
        {
            if (!_storage.IsValidFolder(folder))
            {
                throw new ArgumentException("Invalid folder.");
            }
            string name = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(name) || !CmsFileTypes.IsAllowedFileName(name))
            {
                throw new ArgumentException("File type is not allowed.");
            }

            string targetPath = _storage.BuildManagedPath(folder, name);
            var existing = await _context.Files
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FilePath == targetPath, ct);
            bool inUse = existing != null || _storage.FileNameInUse(folder, name);
            return new CmsFileNameCheckDto
            {
                InUse = inUse,
                SuggestedName = inUse ? _storage.GetAvailableFileName(folder, name) : name,
                ExistingFileGuid = existing?.FileGuid,
                ExistingFriendlyName = existing?.FriendlyName,
                ExistingDeleted = existing?.DeletedOn != null
            };
        }

        public async Task<CmsFileDto> CreateFileAsync(CmsFileCreateRequest request, IFormFile file, CancellationToken ct = default)
        {
            if (!_storage.IsValidFolder(request.Folder))
            {
                throw new ArgumentException("Invalid folder.");
            }
            string uploadName = Path.GetFileName(
                string.IsNullOrWhiteSpace(request.FileName) ? file.FileName : request.FileName);
            if (string.IsNullOrWhiteSpace(uploadName) || !CmsFileTypes.IsAllowedFileName(uploadName))
            {
                throw new ArgumentException("File type is not allowed.");
            }

            bool encrypt = request.Encrypt ?? false;
            string? dbKey = encrypt ? _encryption.GenerateKeyForDb() : null;

            string tempPath = await _storage.SaveToTempAsync(file, ct);
            string finalPath;
            // Overwriting an orphaned on-disk file (no DB row) destroys its bytes; snapshot them first
            // so a later failure can roll the original back instead of leaving nothing in its place.
            string? overwriteBackup = null;
            try
            {
                if (dbKey != null)
                {
                    _encryption.EncryptFileInPlace(tempPath, dbKey);
                }
                if (request.Overwrite == true && _storage.FileNameInUse(request.Folder, uploadName))
                {
                    string targetPath = _storage.BuildManagedPath(request.Folder, uploadName);
                    if (await _context.Files.AnyAsync(f => f.FilePath == targetPath, ct))
                    {
                        throw new InvalidOperationException(
                            $"{uploadName} belongs to an existing file record; replace its content by editing that file.");
                    }
                    if (_storage.ManagedFileExists(targetPath))
                    {
                        overwriteBackup = _storage.BackupManagedFile(targetPath);
                    }
                    _storage.ReplaceInPlace(tempPath, targetPath);
                    finalPath = targetPath;
                }
                else
                {
                    finalPath = _storage.MoveIntoPlace(tempPath, request.Folder, uploadName, makeUnique: false);
                }
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
                // The new bytes are already on disk; reconcile the store (restore an overwritten
                // original, or delete a fresh copy) before failing so nothing is left behind.
                ReconcileStoreAfterFailedCreate(finalPath, overwriteBackup);
                throw new InvalidOperationException($"A file with the name {friendlyName} already exists.");
            }

            var entity = new File
            {
                FileGuid = Guid.NewGuid(),
                FilePath = finalPath,
                Folder = request.Folder,
                FriendlyName = friendlyName,
                Encrypted = encrypt,
                Key = dbKey,
                Description = request.Description ?? string.Empty,
                AllowPublicAccess = request.AllowPublicAccess ?? false,
                OldUrl = NullIfEmpty(request.OldUrl),
                ModifiedOn = DateTime.Now,
                ModifiedBy = CurrentLoginId()
            };

            foreach (var permission in CleanList(request.Permissions))
            {
                entity.FileToPermissions.Add(new Viper.Models.VIPER.FileToPermission { FileGuid = entity.FileGuid, Permission = permission });
            }
            foreach (var iamId in CleanList(request.IamIds))
            {
                entity.FileToPeople.Add(new Viper.Models.VIPER.FileToPerson { FileGuid = entity.FileGuid, IamId = iamId });
            }

            _context.Files.Add(entity);
            _audit.Audit(entity, CmsFileAuditActions.AddFile, BuildCreateDetail(entity));
            _audit.Audit(entity, CmsFileAuditActions.UploadFile, "NewFile");
            bool saved = false;
            try
            {
                await _context.SaveChangesAsync(ct);
                saved = true;
            }
            catch (Exception)
            {
                // A failed save never persisted the record. Drop the pending changes, then reconcile
                // the managed store: restore an overwritten file's original bytes, or remove the
                // freshly stored copy, so nothing is left orphaned or destroyed.
                _context.ChangeTracker.Clear();
                ReconcileStoreAfterFailedCreate(finalPath, overwriteBackup);
                throw;
            }
            finally
            {
                // Only clean up on a committed save (the backup is then obsolete). On a failed save the
                // reconcile above owns the backup: a successful restore already consumed it via Move, and
                // a FAILED restore must keep it as the last copy of the original bytes.
                if (saved && overwriteBackup != null && System.IO.File.Exists(overwriteBackup))
                {
                    System.IO.File.Delete(overwriteBackup);
                }
            }

            var names = await GetNamesByIamIdAsync(entity.FileToPeople.Select(p => p.IamId), ct);
            return CmsFileMapper.ToCmsFileDto(entity, names);
        }

        public async Task<CmsFileDto?> UpdateFileAsync(Guid fileGuid, CmsFileUpdateRequest request, IFormFile? file, CancellationToken ct = default)
        {
            var entity = await LoadFileAsync(fileGuid, tracking: true, ct);
            if (entity == null)
            {
                return null;
            }

            // Validate a replacement upload before anything touches the file on disk, so a bad type
            // can't leave the file half-transformed or strand a backup copy.
            if (file != null)
            {
                ValidateReplacementUpload(entity, file);
            }

            // The crypto toggle and any replacement upload below transform the file on disk before
            // the metadata save. Capture the pre-change state so a failed save can reconcile the
            // on-disk file back to it (mirrors the import service's encryption rollback).
            bool originalEncrypted = entity.Encrypted;
            string? originalKey = entity.Key;

            // A replacement overwrites the bytes on disk before the save commits. Snapshot the
            // original file first (it carries the original encryption state too) so a failed save
            // can roll it back; restoring it supersedes the crypto-only reconcile below.
            string? originalFileBackup = file != null && _storage.ManagedFileExists(entity.FilePath)
                ? _storage.BackupManagedFile(entity.FilePath)
                : null;

            var changes = new List<string>();
            ApplyEncryptionToggle(entity, request.Encrypt ?? false, changes);
            ApplyScalarFieldChanges(entity, request, changes);

            ApplyPermissionDeltas(entity, CleanList(request.Permissions), changes);
            ApplyPersonDeltas(entity, CleanList(request.IamIds), changes);

            if (file != null)
            {
                await WriteReplacementUploadAsync(entity, file, changes, ct);
            }

            entity.ModifiedOn = DateTime.Now;
            entity.ModifiedBy = CurrentLoginId();

            _audit.Audit(entity, CmsFileAuditActions.EditFile, string.Join("; ", changes));
            await SaveUpdateAndReconcileAsync(entity, originalFileBackup, originalEncrypted, originalKey);

            var names = await GetNamesByIamIdAsync(entity.FileToPeople.Select(p => p.IamId), ct);
            return CmsFileMapper.ToCmsFileDto(entity, names);
        }

        /// <summary>
        /// A replacement upload keeps the record's name and path, so its bytes must be an allowed
        /// type and match the original extension; otherwise the stored file would hold mismatched
        /// content and serve a corrupt download. Runs before anything touches disk.
        /// </summary>
        private static void ValidateReplacementUpload(File entity, IFormFile file)
        {
            if (!CmsFileTypes.IsAllowedFileName(file.FileName))
            {
                throw new ArgumentException("File type is not allowed.");
            }
            string replacementExt = Path.GetExtension(entity.FilePath);
            if (!string.Equals(replacementExt, Path.GetExtension(file.FileName), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The replacement file must be the same type as the original ({replacementExt}).");
            }
        }

        /// <summary>
        /// Applies the encryption toggle in place before any replacement upload, so the replacement
        /// is written with the file's final encryption state. Records the change for the audit trail.
        /// </summary>
        private void ApplyEncryptionToggle(File entity, bool encrypt, List<string> changes)
        {
            if (encrypt && !entity.Encrypted)
            {
                string dbKey = _encryption.GenerateKeyForDb();
                if (_storage.ManagedFileExists(entity.FilePath))
                {
                    _encryption.EncryptFileInPlace(entity.FilePath, dbKey);
                }
                entity.Key = dbKey;
                entity.Encrypted = true;
                changes.Add("Encrypted file");
            }
            else if (!encrypt && entity.Encrypted)
            {
                if (!string.IsNullOrEmpty(entity.Key) && _storage.ManagedFileExists(entity.FilePath))
                {
                    _encryption.DecryptFileInPlace(entity.FilePath, entity.Key);
                }
                entity.Key = null;
                entity.Encrypted = false;
                changes.Add("Decrypted file");
            }
        }

        /// <summary>
        /// Applies the description, public-access, and Old URL edits, recording each that actually
        /// changes for the audit trail.
        /// </summary>
        private static void ApplyScalarFieldChanges(File entity, CmsFileUpdateRequest request, List<string> changes)
        {
            string newDescription = request.Description ?? string.Empty;
            if (entity.Description != newDescription)
            {
                entity.Description = newDescription;
                changes.Add("Description updated");
            }
            bool allowPublicAccess = request.AllowPublicAccess ?? false;
            if (entity.AllowPublicAccess != allowPublicAccess)
            {
                entity.AllowPublicAccess = allowPublicAccess;
                changes.Add($"Public access changed to {allowPublicAccess}");
            }
            string? newOldUrl = NullIfEmpty(request.OldUrl);
            if (entity.OldUrl != newOldUrl)
            {
                entity.OldUrl = newOldUrl;
                changes.Add("Old URL updated");
            }
        }

        /// <summary>
        /// Writes a replacement upload over the record's file: encrypts the temp copy to the file's
        /// current state if needed, swaps it in, audits the replacement, and revives a soft-deleted
        /// record (the overwrite-conflict flow). The temp copy is always cleaned up.
        /// </summary>
        private async Task WriteReplacementUploadAsync(File entity, IFormFile file, List<string> changes, CancellationToken ct)
        {
            string tempPath = await _storage.SaveToTempAsync(file, ct);
            try
            {
                if (entity.Encrypted && !string.IsNullOrEmpty(entity.Key))
                {
                    _encryption.EncryptFileInPlace(tempPath, entity.Key);
                }
                _storage.ReplaceInPlace(tempPath, entity.FilePath);
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
            _audit.Audit(entity, CmsFileAuditActions.UploadFile, "ReplacingFile");

            // Uploading new content into a soft-deleted record (the overwrite-conflict flow) makes
            // it live again; metadata-only edits leave deletion state alone.
            if (entity.DeletedOn != null)
            {
                entity.DeletedOn = null;
                changes.Add("Restored file");
            }
        }

        /// <summary>
        /// Persists the metadata edit, reconciling the on-disk file when the save fails: restore the
        /// pre-replacement backup (which also restores the original encryption state) or, with no
        /// replacement, revert just the crypto transform. The backup is deleted once the save commits;
        /// if a restore fails it is left in place as the last copy of the original bytes.
        /// </summary>
        private async Task SaveUpdateAndReconcileAsync(File entity, string? originalFileBackup,
            bool originalEncrypted, string? originalKey)
        {
            bool saved = false;
            try
            {
                // The file on disk may already be in its new encryption state; cancelling the save
                // would strand it without a matching key, so it runs to completion even if aborted.
                await _context.SaveChangesAsync(CancellationToken.None);
                saved = true;
            }
            catch (Exception)
            {
                // Any failure type means the new state was not persisted; drop the pending changes
                // and reconcile the on-disk file back to its original state before rethrowing.
                _context.ChangeTracker.Clear();
                if (originalFileBackup != null)
                {
                    // A replacement overwrote the file; restoring the original bytes also restores
                    // the original encryption state, so the crypto-only reconcile is skipped.
                    RestoreOriginalFileOnSaveFailure(entity, originalFileBackup);
                }
                else
                {
                    RevertCryptoOnSaveFailure(entity, originalEncrypted, originalKey);
                }
                throw;
            }
            finally
            {
                // Only clean up on a committed save (the backup is then obsolete). On a failed save the
                // reconcile above owns the backup: a successful restore already consumed it via Move, and
                // a FAILED restore must keep it as the last copy of the original bytes.
                if (saved && originalFileBackup != null && System.IO.File.Exists(originalFileBackup))
                {
                    System.IO.File.Delete(originalFileBackup);
                }
            }
        }

        /// <summary>
        /// A failed metadata save rolls the database back to the file's original encryption state,
        /// but the on-disk file was already transformed to the new state. Reconcile it back, or a
        /// download would read bytes that don't match the stored key. A failure to revert leaves the
        /// file unreadable, so it is logged as critical rather than swallowed.
        /// </summary>
        private void RevertCryptoOnSaveFailure(File entity, bool originalEncrypted, string? originalKey)
        {
            if (entity.Encrypted == originalEncrypted || !_storage.ManagedFileExists(entity.FilePath))
            {
                return;
            }
            try
            {
                if (originalEncrypted && !string.IsNullOrEmpty(originalKey))
                {
                    // The file had been decrypted; re-encrypt it with the original key.
                    _encryption.EncryptFileInPlace(entity.FilePath, originalKey);
                }
                else if (!originalEncrypted && !string.IsNullOrEmpty(entity.Key))
                {
                    // The file had been encrypted; decrypt it with the key that was generated.
                    _encryption.DecryptFileInPlace(entity.FilePath, entity.Key);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogCritical(ex, "CMS file {FileGuid} could not be reverted after a failed save; "
                    + "its on-disk encryption no longer matches the database.", entity.FileGuid);
            }
        }

        /// <summary>
        /// A failed save after a replacement upload leaves the new bytes on disk while the database
        /// rolled back to the original record. Move the pre-replacement backup back into place so a
        /// download serves the original content (and its original encryption state). A failure to
        /// restore is logged as critical (with the preserved backup path) rather than swallowed; the
        /// caller leaves the backup in place so it remains the last copy of the original bytes.
        /// </summary>
        private void RestoreOriginalFileOnSaveFailure(File entity, string backupPath)
        {
            try
            {
                _storage.ReplaceInPlace(backupPath, entity.FilePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                _logger.LogCritical(ex, "CMS file {FileGuid} could not be restored after a failed save; "
                    + "the on-disk content may not match the database. The original bytes are preserved "
                    + "at {BackupPath} for manual recovery.", entity.FileGuid, backupPath);
            }
        }

        /// <summary>
        /// A create that has already written its bytes but then fails (friendly-name clash or save
        /// error) must not leave the store changed. When the write overwrote an orphaned file, move
        /// its pre-overwrite backup back so the original content survives; otherwise just delete the
        /// freshly stored copy. A failed restore is logged as critical (with the preserved backup
        /// path) rather than swallowed; the caller leaves the backup in place for recovery.
        /// </summary>
        private void ReconcileStoreAfterFailedCreate(string finalPath, string? overwriteBackup)
        {
            if (overwriteBackup == null)
            {
                _storage.DeleteManagedFile(finalPath);
                return;
            }
            try
            {
                _storage.ReplaceInPlace(overwriteBackup, finalPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                _logger.LogCritical(ex, "CMS overwrite at {FilePath} could not be restored after a failed "
                    + "create; the original bytes are preserved at {BackupPath} for manual recovery.",
                    LogSanitizer.SanitizeString(finalPath), overwriteBackup);
            }
        }

        public async Task<bool> SoftDeleteFileAsync(Guid fileGuid, CancellationToken ct = default)
        {
            var entity = await LoadFileAsync(fileGuid, tracking: true, ct);
            if (entity == null)
            {
                return false;
            }
            entity.DeletedOn = DateTime.Now;
            entity.ModifiedOn = DateTime.Now;
            entity.ModifiedBy = CurrentLoginId();
            _audit.Audit(entity, CmsFileAuditActions.DeleteFile, "File Marked for Deletion");
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RestoreFileAsync(Guid fileGuid, CancellationToken ct = default)
        {
            var entity = await LoadFileAsync(fileGuid, tracking: true, ct);
            if (entity == null)
            {
                return false;
            }
            entity.DeletedOn = null;
            entity.ModifiedOn = DateTime.Now;
            entity.ModifiedBy = CurrentLoginId();
            _audit.Audit(entity, CmsFileAuditActions.CancelDelete, "Delete cancelled");
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> PermanentlyDeleteFileAsync(Guid fileGuid, CancellationToken ct = default)
        {
            var entity = await LoadFileAsync(fileGuid, tracking: true, ct);
            if (entity == null)
            {
                return false;
            }

            _audit.Audit(entity, CmsFileAuditActions.DeleteFile, "File Deleted");
            // Remove every dependent row before the File delete: content-block attachments use
            // ClientSetNull (no DB cascade), so a still-attached file would otherwise trip the FK.
            // Purging a still-attached file detaches it from those blocks.
            var blockLinks = await _context.ContentBlockToFiles.Where(l => l.FileGuid == fileGuid).ToListAsync(ct);
            _context.RemoveRange(blockLinks);
            _context.RemoveRange(entity.FileToPermissions);
            _context.RemoveRange(entity.FileToPeople);
            _context.Remove(entity);
            await _context.SaveChangesAsync(ct);

            // The DB row is already gone at this point, so a failed disk delete only strands bytes;
            // log it for manual cleanup rather than failing an operation that did delete the record.
            try
            {
                if (_storage.ManagedFileExists(entity.FilePath))
                {
                    _storage.DeleteManagedFile(entity.FilePath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "CMS file record {FileGuid} was deleted but its bytes at {FilePath} "
                    + "could not be removed from disk.",
                    fileGuid, LogSanitizer.SanitizeString(entity.FilePath));
            }
            return true;
        }

        public async Task<int> PurgeDeletedFilesAsync(int retentionDays, CancellationToken ct = default)
        {
            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var guids = await _context.Files
                .Where(f => f.DeletedOn != null && f.DeletedOn < cutoff)
                .Select(f => f.FileGuid)
                .ToListAsync(ct);

            int purged = 0;
            foreach (var guid in guids)
            {
                try
                {
                    if (await PermanentlyDeleteFileAsync(guid, ct))
                    {
                        purged++;
                    }
                }
                catch (Exception ex) when (ex is DbUpdateException or IOException or InvalidOperationException)
                {
                    // Isolate per-file failures so one bad file doesn't abort the whole purge run.
                    _context.ChangeTracker.Clear();
                    _logger.LogError(ex, "CMS trash purge failed to delete file {FileGuid}", guid);
                }
            }
            return purged;
        }

        private async Task<File?> LoadFileAsync(Guid fileGuid, bool tracking, CancellationToken ct)
        {
            var query = _context.Files
                .Include(f => f.FileToPermissions)
                .Include(f => f.FileToPeople)
                .AsSplitQuery();
            if (!tracking)
            {
                query = query.AsNoTracking();
            }
            var file = await query.FirstOrDefaultAsync(f => f.FileGuid == fileGuid, ct);
            if (file != null)
            {
                file.FilePath = NormalizeRootFolder(file.FilePath);
            }
            return file;
        }

        /// <summary>
        /// Fix up the storage root if the record was created on another environment
        /// (e.g. a dev path on prod), mirroring Data.CMS.ReplaceRootFolder but guarded
        /// against paths that don't contain a \Files segment.
        /// </summary>
        private string NormalizeRootFolder(string filePath)
        {
            string root = _storage.RootFolder;
            if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                int filesIndex = filePath.IndexOf(@"\Files\", StringComparison.OrdinalIgnoreCase);
                if (filesIndex >= 0)
                {
                    return root + filePath[(filesIndex + @"\Files".Length)..];
                }
            }
            return filePath;
        }

        private void ApplyPermissionDeltas(File entity, List<string> requested, List<string> changes)
        {
            var existing = entity.FileToPermissions.ToList();
            foreach (var fp in existing.Where(fp => !requested.Contains(fp.Permission, StringComparer.OrdinalIgnoreCase)))
            {
                entity.FileToPermissions.Remove(fp);
                _context.Remove(fp);
                changes.Add($"Permission removed: {fp.Permission}");
            }
            var existingNames = existing.Select(p => p.Permission).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var permission in requested.Where(p => !existingNames.Contains(p)))
            {
                entity.FileToPermissions.Add(new Viper.Models.VIPER.FileToPermission { FileGuid = entity.FileGuid, Permission = permission });
                changes.Add($"Permission added: {permission}");
            }
        }

        private void ApplyPersonDeltas(File entity, List<string> requested, List<string> changes)
        {
            var existing = entity.FileToPeople.ToList();
            foreach (var fp in existing.Where(fp => !requested.Contains(fp.IamId, StringComparer.OrdinalIgnoreCase)))
            {
                entity.FileToPeople.Remove(fp);
                _context.Remove(fp);
                changes.Add($"Person removed: {fp.IamId}");
            }
            var existingIds = existing.Select(p => p.IamId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var iamId in requested.Where(p => !existingIds.Contains(p)))
            {
                entity.FileToPeople.Add(new Viper.Models.VIPER.FileToPerson { FileGuid = entity.FileGuid, IamId = iamId });
                changes.Add($"Person added: {iamId}");
            }
        }

        private async Task<Dictionary<string, string>> GetNamesByIamIdAsync(IEnumerable<string> iamIds, CancellationToken ct)
        {
            var ids = iamIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<string, string>();
            }
            return await _aaudContext.AaudUsers
                .AsNoTracking()
                .Where(u => u.IamId != null && EF.Parameter(ids).Contains(u.IamId))
                .GroupBy(u => u.IamId!)
                .Select(g => new { IamId = g.Key, Name = g.Select(u => u.DisplayFullName).First() })
                .ToDictionaryAsync(x => x.IamId, x => x.Name, ct);
        }

        private string CurrentLoginId()
        {
            return _userHelper.GetCurrentUser()?.LoginId ?? "unknown";
        }

        private static string BuildCreateDetail(File entity)
        {
            var parts = new List<string> { $"Folder: {entity.Folder}" };
            if (entity.AllowPublicAccess)
            {
                parts.Add("Public access: true");
            }
            if (entity.Encrypted)
            {
                parts.Add("Encrypted: true");
            }
            if (entity.FileToPermissions.Count > 0)
            {
                parts.Add("Permissions: " + string.Join(", ", entity.FileToPermissions.Select(p => p.Permission)));
            }
            if (entity.FileToPeople.Count > 0)
            {
                parts.Add("People: " + string.Join(", ", entity.FileToPeople.Select(p => p.IamId)));
            }
            return string.Join("; ", parts);
        }

        private static List<string> CleanList(List<string> values)
        {
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
