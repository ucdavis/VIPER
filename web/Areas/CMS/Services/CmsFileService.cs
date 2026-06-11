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

        public CmsFileService(VIPERContext context, AAUDContext aaudContext, ICmsFileStorageService storage,
            ICmsFileEncryptionService encryption, ICmsFileAuditService audit, IUserHelper userHelper)
        {
            _context = context;
            _aaudContext = aaudContext;
            _storage = storage;
            _encryption = encryption;
            _audit = audit;
            _userHelper = userHelper;
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

            query = status.ToLowerInvariant() switch
            {
                "active" => query.Where(f => f.DeletedOn == null),
                "deleted" => query.Where(f => f.DeletedOn != null),
                _ => query
            };

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

        public async Task<CmsFileDto> CreateFileAsync(CmsFileCreateRequest request, IFormFile file, CancellationToken ct = default)
        {
            if (!_storage.IsValidFolder(request.Folder))
            {
                throw new ArgumentException("Invalid folder.");
            }
            if (!CmsFileTypes.IsAllowedFileName(file.FileName))
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
                finalPath = _storage.MoveIntoPlace(tempPath, request.Folder, file.FileName, request.MakeUnique ?? false);
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }

            string friendlyName = BuildFriendlyName(request.Folder, Path.GetFileName(finalPath));
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
            await _context.SaveChangesAsync(ct);

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
            }

            entity.ModifiedOn = DateTime.Now;
            entity.ModifiedBy = CurrentLoginId();

            _audit.Audit(entity, CmsFileAuditActions.EditFile, string.Join("; ", changes));
            await _context.SaveChangesAsync(ct);

            var names = await GetNamesByIamIdAsync(entity.FileToPeople.Select(p => p.IamId), ct);
            return CmsFileMapper.ToCmsFileDto(entity, names);
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

        private static string BuildFriendlyName(string folder, string fileName)
        {
            return folder.Replace('\\', '-').Replace('/', '-') + "-" + fileName;
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
