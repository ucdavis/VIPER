using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Classes.SQLContext;
using File = Viper.Models.VIPER.File;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsFileService
    {
        Task<(List<CmsFileDto> Files, int Total)> GetFilesAsync(string? folder, string status, string? search,
            bool? encrypted, bool? isPublic, int page, int perPage, string? sortBy, bool descending, CancellationToken ct = default);

        Task<List<CmsFolderCountDto>> GetFolderCountsAsync(string status, bool? encrypted, bool? isPublic, CancellationToken ct = default);

        Task<CmsFileNameCheckDto> CheckNameAsync(string folder, string fileName, CancellationToken ct = default);

        Task<CmsFileDto?> GetFileAsync(Guid fileGuid, CancellationToken ct = default);

        Task<CmsFileDto> CreateFileAsync(CmsFileCreateRequest request, IFormFile file, CancellationToken ct = default);

        Task<CmsFileDto?> UpdateFileAsync(Guid fileGuid, CmsFileUpdateRequest request, IFormFile? file, CancellationToken ct = default);

        Task<bool> SoftDeleteFileAsync(Guid fileGuid, CancellationToken ct = default);

        Task<bool> RestoreFileAsync(Guid fileGuid, CancellationToken ct = default);

        Task<bool> PermanentlyDeleteFileAsync(Guid fileGuid, CancellationToken ct = default);
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
            bool? encrypted, bool? isPublic, int page, int perPage, string? sortBy, bool descending, CancellationToken ct = default)
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

            query = ApplyStatusFilter(query, status);

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
            CancellationToken ct = default)
        {
            var query = _context.Files
                .AsNoTracking()
                .TagWith("CmsFileService.GetFolderCounts")
                .AsQueryable();

            query = ApplyStatusFilter(query, status);

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

        private static IQueryable<File> ApplyStatusFilter(IQueryable<File> query, string? status)
        {
            return status?.ToLowerInvariant() switch
            {
                "active" => query.Where(f => f.DeletedOn == null),
                "deleted" => query.Where(f => f.DeletedOn != null),
                _ => query
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
                _storage.DeleteManagedFile(finalPath);
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
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception)
            {
                // The upload is already in the managed store; any failed save would orphan it on
                // disk, so drop the pending changes and remove the stored copy before rethrowing.
                _context.ChangeTracker.Clear();
                _storage.DeleteManagedFile(finalPath);
                throw;
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

            var changes = new List<string>();
            bool encrypt = request.Encrypt ?? false;
            bool allowPublicAccess = request.AllowPublicAccess ?? false;

            // The crypto toggle and any replacement upload below transform the file on disk before
            // the metadata save. Capture the pre-change state so a failed save can reconcile the
            // on-disk file back to it (mirrors the import service's encryption rollback).
            bool originalEncrypted = entity.Encrypted;
            string? originalKey = entity.Key;

            // Encryption toggle happens before any file replacement so the replacement upload
            // is written with the file's final encryption state.
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

            string newDescription = request.Description ?? string.Empty;
            if (entity.Description != newDescription)
            {
                entity.Description = newDescription;
                changes.Add("Description updated");
            }
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

            ApplyPermissionDeltas(entity, CleanList(request.Permissions), changes);
            ApplyPersonDeltas(entity, CleanList(request.IamIds), changes);

            if (file != null)
            {
                if (!CmsFileTypes.IsAllowedFileName(file.FileName))
                {
                    throw new ArgumentException("File type is not allowed.");
                }
                // The replacement keeps the existing record's name and path, so its bytes must
                // stay the same type; a mismatched extension would leave the stored .pdf holding
                // .png bytes and serve a corrupt download.
                string currentExt = Path.GetExtension(entity.FilePath);
                if (!string.Equals(currentExt, Path.GetExtension(file.FileName), StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"The replacement file must be the same type as the original ({currentExt}).");
                }
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

                // Uploading new content into a soft-deleted record (the overwrite-conflict
                // flow) makes it live again; metadata-only edits leave deletion state alone.
                if (entity.DeletedOn != null)
                {
                    entity.DeletedOn = null;
                    changes.Add("Restored file");
                }
            }

            entity.ModifiedOn = DateTime.Now;
            entity.ModifiedBy = CurrentLoginId();

            _audit.Audit(entity, CmsFileAuditActions.EditFile, string.Join("; ", changes));
            try
            {
                // The file on disk may already be in its new encryption state; cancelling the save
                // would strand it without a matching key, so it runs to completion even if aborted.
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception)
            {
                // Any failure type means the new state was not persisted; drop the pending changes
                // and reconcile the on-disk file back to its original encryption state before rethrowing.
                _context.ChangeTracker.Clear();
                RevertCryptoOnSaveFailure(entity, originalEncrypted, originalKey);
                throw;
            }

            var names = await GetNamesByIamIdAsync(entity.FileToPeople.Select(p => p.IamId), ct);
            return CmsFileMapper.ToCmsFileDto(entity, names);
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
            _context.RemoveRange(entity.FileToPermissions);
            _context.RemoveRange(entity.FileToPeople);
            _context.Remove(entity);
            await _context.SaveChangesAsync(ct);

            if (_storage.ManagedFileExists(entity.FilePath))
            {
                _storage.DeleteManagedFile(entity.FilePath);
            }
            return true;
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
