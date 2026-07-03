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
            // DirectoryInfo.Name gives the (non-null) leaf folder name directly, avoiding the
            // Path.GetFileName nullable return that would otherwise need coalescing.
            return new DirectoryInfo(RootFolder).GetDirectories()
                .Select(d => d.Name)
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
                    || HasInvalidNameChar(s)))
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
            if (File.Exists(targetPath))
            {
                return true;
            }
            // Also check the DB in case a record exists whose disk file is missing; a new file
            // at that path would be served under the orphaned record's permissions. Records
            // created under another environment's storage root (ReplaceRootFolder rewrites
            // them at read time) claim the same folder+name, so match the path suffix in
            // either separator style rather than only the exact current-root path.
            string[] segments = folder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            string leaf = GetLeafName(fileName);
            string suffixBack = "\\" + string.Join('\\', segments) + "\\" + leaf;
            string suffixFwd = "/" + string.Join('/', segments) + "/" + leaf;
            return _context.Files.Any(f => f.FilePath == targetPath
                || f.FilePath.EndsWith(suffixBack) || f.FilePath.EndsWith(suffixFwd));
        }

        public string GetAvailableFileName(string folder, string fileName, IReadOnlySet<string>? reservedNames = null)
        {
            string finalName = GetLeafName(fileName);
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

            string finalName = GetLeafName(fileName);
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
            File.Move(tempPath, targetPath);
            return targetPath;
        }

        public void ReplaceInPlace(string tempPath, string existingFilePath)
        {
            AssertUnderRoot(existingFilePath);
            File.Move(tempPath, existingFilePath, overwrite: true);
        }

        public string BackupManagedFile(string filePath)
        {
            AssertUnderRoot(filePath);
            string tempFolder = Path.Join(Path.GetTempPath(), "Viper-CMS-Uploads");
            System.IO.Directory.CreateDirectory(tempFolder);
            string backupPath = Path.Join(tempFolder, Guid.NewGuid().ToString("N"));
            File.Copy(filePath, backupPath, overwrite: true);
            return backupPath;
        }

        public void DeleteManagedFile(string filePath)
        {
            AssertUnderRoot(filePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public bool ManagedFileExists(string filePath)
        {
            return IsUnderRoot(filePath) && File.Exists(filePath);
        }

        private string GetUniqueFileName(string folder, string fileName, IReadOnlySet<string>? reservedNames = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            // Preload the folder's taken names once (one disk listing + one DB query) so the
            // legacy _0.._999 probe loops in memory instead of issuing a query per candidate.
            var taken = GetTakenLeafNames(folder);
            for (int i = 0; i < 1000; i++)
            {
                string candidate = $"{baseName}_{i}{extension}";
                if (!taken.Contains(candidate)
                    && (reservedNames == null || !reservedNames.Contains(candidate))
                    // Final single-name confirmation guards the race with a concurrent upload.
                    && !FileNameInUse(folder, candidate))
                {
                    return candidate;
                }
            }
            throw new InvalidOperationException($"Unable to generate a unique name for {fileName} in {folder}.");
        }

        /// <summary>
        /// Every leaf name already claimed in a folder: files on disk plus DB records whose
        /// Folder matches in either separator style (their FilePath may carry another
        /// environment's storage root, so the path itself is not compared).
        /// </summary>
        private HashSet<string> GetTakenLeafNames(string folder)
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] segments = folder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            string dirPath = Path.GetFullPath(Path.Join(RootFolder, Path.Join(segments)));
            // System.IO qualified: the sibling Viper.Areas.Directory namespace shadows it here.
            if (IsUnderRoot(dirPath) && System.IO.Directory.Exists(dirPath))
            {
                foreach (var leaf in System.IO.Directory.EnumerateFiles(dirPath).Select(Path.GetFileName))
                {
                    if (!string.IsNullOrEmpty(leaf))
                    {
                        taken.Add(leaf);
                    }
                }
            }

            string canonBack = string.Join('\\', segments);
            string canonFwd = string.Join('/', segments);
            var dbPaths = _context.Files
                .Where(f => f.Folder == canonBack || f.Folder == canonFwd)
                .Select(f => f.FilePath)
                .ToList();
            foreach (var path in dbPaths)
            {
                taken.Add(CmsFileNaming.GetLeafName(path));
            }
            return taken;
        }

        private string BuildTargetPath(string folder, string fileName)
        {
            // Split the folder on both separator styles and rejoin with the host separator so a
            // user-supplied "sub\dir" resolves to real nested folders on Linux too (Path.Join would
            // otherwise treat "sub\dir" as one literal directory name there).
            string[] folderSegments = folder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            string path = Path.GetFullPath(Path.Join(RootFolder, Path.Join(folderSegments), GetLeafName(fileName)));
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
            return fullPath.StartsWith(root, CmsFilePathSafety.PathComparison);
        }

        // Both-separator leaf extraction lives in CmsFileNaming so callers outside the
        // storage service (e.g. the check-name endpoint) strip path components identically.
        private static string GetLeafName(string name)
        {
            return CmsFileNaming.GetLeafName(name);
        }

        // Reject on an explicit, OS-independent set rather than Path.GetInvalidFileNameChars, which
        // on Linux returns only { '\0', '/' } and would let '<', '>', ':' etc. through. Stricter than
        // the host filesystem on purpose (defense in depth).
        private static readonly char[] InvalidNameChars = ['<', '>', ':', '"', '|', '?', '*'];

        private static bool HasInvalidNameChar(string segment)
        {
            return segment.Any(c => c < ' ' || Array.IndexOf(InvalidNameChars, c) >= 0);
        }
    }
}
