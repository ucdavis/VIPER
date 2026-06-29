using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Constants;
using Viper.Classes.SQLContext;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsFileStorageService
    {
        string RootFolder { get; }

        /// <summary>Top-level folders ("VIPER apps") that files can be stored under.</summary>
        List<string> GetTopLevelFolders();

        /// <summary>
        /// Top-level folders for FILTERING: the upload allow-list (disk) unioned with
        /// folders that only exist on file records (e.g. created on another environment's
        /// disk). Not valid as upload destinations.
        /// </summary>
        Task<List<string>> GetFilterFoldersAsync(CancellationToken ct = default);

        /// <summary>
        /// Validate a user-supplied folder (may contain subfolders, e.g. "cats\photos").
        /// The first segment must be an existing top-level folder and the resolved path
        /// must stay inside the root.
        /// </summary>
        bool IsValidFolder(string folder);

        /// <summary>True if the name exists on disk or in the files table for this folder.</summary>
        bool FileNameInUse(string folder, string fileName);

        /// <summary>
        /// The name MoveIntoPlace would store folder\fileName under: the name itself when free,
        /// otherwise the first free name_0..name_999 candidate. <paramref name="reservedNames"/>
        /// (optional) are treated as already taken, so callers planning several writes in one
        /// batch get the same unique names the sequential moves would actually assign.
        /// </summary>
        string GetAvailableFileName(string folder, string fileName, IReadOnlySet<string>? reservedNames = null);

        /// <summary>Resolved managed path for folder\fileName (validated to stay under the root).</summary>
        string BuildManagedPath(string folder, string fileName);

        /// <summary>Save an upload to a temp file (outside the storage root); returns the temp path.</summary>
        Task<string> SaveToTempAsync(IFormFile file, CancellationToken ct = default);

        /// <summary>
        /// Move a temp file into the storage root at folder\fileName. On name conflict, either
        /// auto-rename (name_0..name_999) or throw, per <paramref name="makeUnique"/>.
        /// Returns the final absolute path.
        /// </summary>
        string MoveIntoPlace(string tempPath, string folder, string fileName, bool makeUnique);

        /// <summary>Overwrite the managed file at <paramref name="existingFilePath"/> with a temp file.</summary>
        void ReplaceInPlace(string tempPath, string existingFilePath);

        /// <summary>
        /// Copy a managed file to a temp backup outside the storage root and return its path. Pair
        /// with ReplaceInPlace(backupPath, originalPath) to roll the original bytes back into place
        /// after a failed save.
        /// </summary>
        string BackupManagedFile(string filePath);

        /// <summary>Delete a file, verifying it lives under the storage root first.</summary>
        void DeleteManagedFile(string filePath);

        bool ManagedFileExists(string filePath);
    }

    /// <summary>
    /// On-disk storage for CMS-managed files. User input never becomes a path without
    /// validation: folders are checked against the top-level folder list and resolved-path
    /// containment, and file names are stripped to their final segment (see PLAN-CMS.md §11.7).
    /// </summary>
    public class CmsFileStorageService : ICmsFileStorageService
    {
        private readonly VIPERContext _context;

        public string RootFolder { get; }

        public CmsFileStorageService(VIPERContext context, IConfiguration configuration)
        {
            _context = context;
            RootFolder = configuration["CMS:FileStorageRoot"] ?? Data.CMS.GetRootFileFolder();
        }

        public List<string> GetTopLevelFolders()
        {
            if (!System.IO.Directory.Exists(RootFolder))
            {
                return new List<string>();
            }
            return System.IO.Directory.GetDirectories(RootFolder)
                .Select(d => Path.GetFileName(d) ?? string.Empty)
                .Where(d => !string.IsNullOrEmpty(d))
                .OrderBy(d => d)
                .ToList();
        }

        public async Task<List<string>> GetFilterFoldersAsync(CancellationToken ct = default)
        {
            var dbFolders = await _context.Files.Select(f => f.Folder).Distinct().ToListAsync(ct);
            var topLevel = dbFolders
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f!.Split(['\\', '/'], StringSplitOptions.None)[0]);
            return GetTopLevelFolders()
                .Union(topLevel, StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool IsValidFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return false;
            }

            var segments = folder.Split(['\\', '/'], StringSplitOptions.None);
            if (segments.Any(s => string.IsNullOrWhiteSpace(s) || s == "." || s == ".."
                    || s.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                return false;
            }

            // First segment must be an existing top-level folder (legacy rule); subfolders
            // may be created on demand by MoveIntoPlace.
            if (!GetTopLevelFolders().Any(f => string.Equals(f, segments[0], StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return IsUnderRoot(Path.Join(RootFolder, Path.Join(segments)));
        }

        public bool FileNameInUse(string folder, string fileName)
        {
            string targetPath = BuildTargetPath(folder, fileName);
            if (System.IO.File.Exists(targetPath))
            {
                return true;
            }
            // Also check the DB in case a record exists whose disk file is missing; a new file
            // at that path would be served under the orphaned record's permissions.
            return _context.Files.Any(f => f.FilePath == targetPath);
        }

        public string GetAvailableFileName(string folder, string fileName, IReadOnlySet<string>? reservedNames = null)
        {
            string finalName = Path.GetFileName(fileName);
            bool taken = FileNameInUse(folder, finalName)
                || (reservedNames != null && reservedNames.Contains(finalName));
            return taken ? GetUniqueFileName(folder, finalName, reservedNames) : finalName;
        }

        public string BuildManagedPath(string folder, string fileName)
        {
            return BuildTargetPath(folder, fileName);
        }

        public async Task<string> SaveToTempAsync(IFormFile file, CancellationToken ct = default)
        {
            string tempFolder = Path.Join(Path.GetTempPath(), "Viper-CMS-Uploads");
            System.IO.Directory.CreateDirectory(tempFolder);
            string tempPath = Path.Join(tempFolder, Guid.NewGuid().ToString("N"));
            await using FileStream stream = new(tempPath, FileMode.CreateNew);
            await file.CopyToAsync(stream, ct);
            return tempPath;
        }

        public string MoveIntoPlace(string tempPath, string folder, string fileName, bool makeUnique)
        {
            if (!IsValidFolder(folder))
            {
                throw new ArgumentException($"Invalid folder", nameof(folder));
            }

            string finalName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(finalName) || !CmsFileTypes.IsAllowedFileName(finalName))
            {
                throw new ArgumentException("Invalid file name", nameof(fileName));
            }

            if (FileNameInUse(folder, finalName))
            {
                if (!makeUnique)
                {
                    throw new InvalidOperationException($"File {finalName} already exists in folder {folder}.");
                }
                finalName = GetUniqueFileName(folder, finalName);
            }

            string targetPath = BuildTargetPath(folder, finalName);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            System.IO.File.Move(tempPath, targetPath);
            return targetPath;
        }

        public void ReplaceInPlace(string tempPath, string existingFilePath)
        {
            AssertUnderRoot(existingFilePath);
            System.IO.File.Move(tempPath, existingFilePath, overwrite: true);
        }

        public string BackupManagedFile(string filePath)
        {
            AssertUnderRoot(filePath);
            string tempFolder = Path.Join(Path.GetTempPath(), "Viper-CMS-Uploads");
            System.IO.Directory.CreateDirectory(tempFolder);
            string backupPath = Path.Join(tempFolder, Guid.NewGuid().ToString("N"));
            System.IO.File.Copy(filePath, backupPath, overwrite: true);
            return backupPath;
        }

        public void DeleteManagedFile(string filePath)
        {
            AssertUnderRoot(filePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        public bool ManagedFileExists(string filePath)
        {
            return IsUnderRoot(filePath) && System.IO.File.Exists(filePath);
        }

        private string GetUniqueFileName(string folder, string fileName, IReadOnlySet<string>? reservedNames = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            // Match legacy behavior: append _0, _1, ... _999 until unique.
            for (int i = 0; i < 1000; i++)
            {
                string candidate = $"{baseName}_{i}{extension}";
                if (!FileNameInUse(folder, candidate)
                    && (reservedNames == null || !reservedNames.Contains(candidate)))
                {
                    return candidate;
                }
            }
            throw new InvalidOperationException($"Unable to generate a unique name for {fileName} in {folder}.");
        }

        private string BuildTargetPath(string folder, string fileName)
        {
            string path = Path.GetFullPath(Path.Join(RootFolder, folder, Path.GetFileName(fileName)));
            AssertUnderRoot(path);
            return path;
        }

        private void AssertUnderRoot(string filePath)
        {
            if (!IsUnderRoot(filePath))
            {
                throw new ArgumentException("Path is outside the CMS file storage root.", nameof(filePath));
            }
        }

        private bool IsUnderRoot(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            string root = Path.GetFullPath(RootFolder + Path.DirectorySeparatorChar);
            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }
    }
}
