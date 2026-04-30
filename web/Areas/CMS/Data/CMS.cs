using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Models;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.AAUD;
using Viper.Models.VIPER;
using Viper.Services;
using File = Viper.Models.VIPER.File;

namespace Viper.Areas.CMS.Data
{
    public class CMS : ICMS
    {
        #region Properties (private/public)
        private readonly VIPERContext? _viperContext;
        private readonly RAPSContext? _rapsContext;
        private readonly IHtmlSanitizerService _sanitizerService;
        private readonly ILogger<CMS>? _logger;

        public IUserHelper UserHelper { get; set; }

        public Dictionary<string, string> MimeTypes { get; set; } = new()
        {
            ["pdf"] = "application/pdf",
            ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ["doc"] = "application/msword",
            ["xls"] = "application/vnd.ms-excel",
            ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ["csv"] = "text/csv",
            ["ppt"] = "application/vnd.ms-powerpoint",
            ["pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ["pptm"] = "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            ["txt"] = "text/plain",
            ["html"] = "application/xhtml+xml",
            ["gif"] = "image/gif",
            ["png"] = "image/png",
            ["jpg"] = "image/jpeg",
            ["jpeg"] = "image/jpeg",
            ["tiff"] = "image/tiff",
            ["mp3"] = "audio/mpeg",
            ["wav"] = "audio/wav",
            ["mp4"] = "video/mp4",
            ["webm"] = "video/webm",
            ["oft"] = "application/vnd.ms-outlook",
            ["eps"] = "application/postscript",
            ["zip"] = "application/zip",
            ["7z"] = "application/x-7z-compressed",
            ["dmg"] = "application/x-apple-diskimage",
            ["exe"] = "application/vnd.microsoft.portable-executable"
        };

        #endregion

        #region Constructors
        public CMS(VIPERContext viperContext, RAPSContext rapsContext, IHtmlSanitizerService sanitizerService, ILogger<CMS>? logger = null)
        {
            this._viperContext = viperContext;
            this._rapsContext = rapsContext;
            this._sanitizerService = sanitizerService;
            this._logger = logger;
            UserHelper = new UserHelper();
        }
        #endregion

        #region public IEnumerable<ContentBlock>? GetContentBlocksAllowed(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status)
        /// <summary>
        /// Get content blocks and filter based on permissions
        /// </summary>
        /// <returns>List of blocks</returns>
        public IEnumerable<ContentBlock>? GetContentBlocksAllowed(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status)
        {
            // Fetch raw (no sanitization): we sanitize after the permission filter so we don't
            // waste work on blocks the current user isn't allowed to see.
            var blocks = FetchContentBlocks(contentBlockID, friendlyName, system, viperSectionPath, page, blockOrder, allowPublicAccess, status);

            AaudUser? currentUser = UserHelper.GetCurrentUser();
            List<ContentBlock> goodBlocks = new();

            if (blocks != null && _rapsContext != null)
            {
                foreach (var b in blocks)
                {
                    var hasAccess = b.AllowPublicAccess; //block is available without authentication
                    if (!hasAccess && currentUser != null)
                    {
                        hasAccess =
                            //CMS admin
                            UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.CMS.ManageContentBlocks") ||
                            //available to all logged in users
                            b.ContentBlockToPermissions.Count == 0 && UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure") ||
                            //available due to having specific permission(s)
                            b.ContentBlockToPermissions.Count > 0 && b.ContentBlockToPermissions
                                .Any(cp => UserHelper.GetAllPermissions(_rapsContext, currentUser)
                                    .Any(p => string.Compare(cp.Permission, p.Permission, true) == 0));
                    }
                    // only include blocks that the user has permission to see
                    if (hasAccess)
                    {
                        goodBlocks.Add(b);
                    }
                }

                SanitizeContentBlocks(goodBlocks);
                return goodBlocks;

            }

            return null;
        }
        #endregion

        #region public IEnumerable<ContentBlock>? GetContentBlocks(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status)
        /// <summary>
        /// Get content blocks without filtering on permissions
        /// </summary>
        /// <returns>List of blocks</returns>
        public IEnumerable<ContentBlock>? GetContentBlocks(int? contentBlockID = null, string? friendlyName = null, string? system = null,
            string? viperSectionPath = null, string? page = null, int? blockOrder = null,
            bool? allowPublicAccess = null, int? status = null)
        {
            var blocks = FetchContentBlocks(contentBlockID, friendlyName, system, viperSectionPath, page, blockOrder, allowPublicAccess, status);
            if (blocks != null)
            {
                SanitizeContentBlocks(blocks);
            }
            return blocks;
        }
        #endregion

        // AsNoTracking because SanitizeContentBlocks mutates b.Content; a later SaveChanges on a
        // tracked entity (e.g. DeleteContentBlock setting State=Modified) would otherwise persist
        // the sanitized HTML back to the DB as a side-effect of a read.
        private List<ContentBlock>? FetchContentBlocks(int? contentBlockID, string? friendlyName, string? system,
            string? viperSectionPath, string? page, int? blockOrder,
            bool? allowPublicAccess, int? status)
        {
            return _viperContext?.ContentBlocks
                    .AsNoTracking()
                    .Include(p => p.ContentBlockToPermissions)
                    .Include(f => f.ContentBlockToFiles)
                        .ThenInclude(cbf => cbf.File)
                    .Include(h => h.ContentHistories)
                    .Where(c => c.ContentBlockId.Equals(contentBlockID) || contentBlockID == null)
                    .Where(c => string.IsNullOrEmpty(c.FriendlyName) ? string.IsNullOrEmpty(friendlyName) : c.FriendlyName.Equals(friendlyName) || string.IsNullOrEmpty(friendlyName))
                    .Where(c => c.System.Equals(system) || string.IsNullOrEmpty(system))
                    .Where(c => string.IsNullOrEmpty(c.ViperSectionPath) ? string.IsNullOrEmpty(viperSectionPath) : c.ViperSectionPath.Equals(viperSectionPath) || string.IsNullOrEmpty(viperSectionPath))
                    .Where(c => string.IsNullOrEmpty(c.Page) ? string.IsNullOrEmpty(page) : c.Page.Equals(page) || string.IsNullOrEmpty(page))
                    .Where(c => c.BlockOrder.Equals(blockOrder) || blockOrder == null)
                    .Where(c => c.AllowPublicAccess.Equals(allowPublicAccess) || allowPublicAccess == null)
                    .Where(c => (c.DeletedOn == null && status == 1) || (c.DeletedOn != null && status == 0) || status == null)
                    .OrderBy(c => c.BlockOrder)
                    .AsSplitQuery()
                    .ToList();
        }

        private void SanitizeContentBlocks(IEnumerable<ContentBlock> blocks)
        {
            foreach (var b in blocks)
            {
                b.Content = _sanitizerService.Sanitize(b.Content);
            }
        }

        #region public CMSFile? GetFile(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name)
        /// <summary>
        /// Returns the first file that matches the parameters past (or null)
        /// </summary>
        /// <returns>File object</returns>
        public CMSFile? GetFile(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name)
        {
            // Dispatch to a per-identifier method so each query shape gets its own cached plan,
            // avoiding the catch-all (@P IS NULL OR col = @P) antipattern. See VPR-143.
            if (!string.IsNullOrEmpty(fileGUID) && Guid.TryParse(fileGUID, out var guid))
            {
                return GetFileByGuid(guid);
            }

            if (!string.IsNullOrEmpty(oldURL))
            {
                return GetFileByOldUrl(oldURL);
            }

            if (!string.IsNullOrEmpty(friendlyName))
            {
                return GetFileByFriendlyName(friendlyName);
            }

            if (!string.IsNullOrEmpty(folder) || !string.IsNullOrEmpty(name))
            {
                return GetFileByFolderAndName(folder ?? string.Empty, name ?? string.Empty);
            }

            return null;
        }

        public CMSFile? GetFileByGuid(Guid fileGuid)
        {
            var file = _viperContext?.Files
                .Include(p => p.FileToPermissions)
                .Include(p => p.FileToPeople)
                .AsSplitQuery()
                .TagWith("CMS.GetFileByGuid")
                .FirstOrDefault(f => f.FileGuid == fileGuid);

            return ToCMSFile(file);
        }

        public CMSFile? GetFileByOldUrl(string oldUrl)
        {
            var file = _viperContext?.Files
                .Include(p => p.FileToPermissions)
                .Include(p => p.FileToPeople)
                .AsSplitQuery()
                .TagWith("CMS.GetFileByOldUrl")
                .FirstOrDefault(f => f.OldUrl == oldUrl);

            return ToCMSFile(file);
        }

        public CMSFile? GetFileByFriendlyName(string friendlyName)
        {
            var file = _viperContext?.Files
                .Include(p => p.FileToPermissions)
                .Include(p => p.FileToPeople)
                .AsSplitQuery()
                .TagWith("CMS.GetFileByFriendlyName")
                .FirstOrDefault(f => f.FriendlyName == friendlyName);

            return ToCMSFile(file);
        }

        public CMSFile? GetFileByFolderAndName(string folder, string name)
        {
            var filePath = folder + @"\" + name;
            var file = _viperContext?.Files
                .Include(p => p.FileToPermissions)
                .Include(p => p.FileToPeople)
                .AsSplitQuery()
                .TagWith("CMS.GetFileByFolderAndName")
                .FirstOrDefault(f => f.FilePath == filePath);

            return ToCMSFile(file);
        }

        private static CMSFile? ToCMSFile(File? file)
        {
            if (file is null)
            {
                return null;
            }

            var cmsf = new CMSFile(file);
            ReplaceRootFolder(cmsf);
            return cmsf;
        }
        #endregion

        #region public IEnumerable<CMSFile> GetAllFiles(string? folder, bool? isPublic, string? search, string? status, bool? encrypted)
        /// <summary>
        /// Search for matching files
        /// </summary>
        public IEnumerable<CMSFile> GetAllFiles(string? folder, bool? isPublic, string? search, string? status, bool? encrypted)
        {

            // get files based on paramenters
            var files = _viperContext?.Files
                    .Include(p => p.FileToPermissions)
                    .Include(p => p.FileToPeople)
                    .Where(f => (string.IsNullOrEmpty(folder) && f.FilePath.Contains(folder + @"\")) || string.IsNullOrEmpty(folder))
                    .Where(f => f.AllowPublicAccess.Equals(isPublic) || isPublic == null)
                    .Where(c => (string.IsNullOrEmpty(status) || (c.DeletedOn == null && status.ToLower() != "active") || (c.DeletedOn != null && status.ToLower() == "active")))
                    .Where(f => f.Encrypted.Equals(encrypted) || encrypted == null)
                    .Where(f => (string.IsNullOrEmpty(search) || f.FriendlyName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        f.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrEmpty(f.OldUrl) ? string.IsNullOrEmpty(search) : f.OldUrl.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        f.FilePath.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.FriendlyName)
                .AsSplitQuery()
                .ToList();

            if (files != null)
            {
                List<CMSFile> cmslist = new();
                foreach (var f in files)
                {
                    CMSFile cmsf = new(f);
                    cmslist.Add(cmsf);
                    ReplaceRootFolder(cmsf);
                }

                return cmslist;
            }

            return new List<CMSFile>();

        }
        #endregion

        #region public static string GetFriendlyURL(string friendlyName, bool allowPublicAccess = false)
        /// <summary>
        /// Get Friendly URL for a friendly name. Currently, always points to ColdFusion Viper
        /// </summary>
        public static string GetFriendlyURL(string friendlyName, bool allowPublicAccess = false)
        {
            string rootURL = String.Empty;
            HttpRequest? thisRequest = HttpHelper.HttpContext?.Request;

            if (thisRequest != null)
            {
                Uri url = new(thisRequest.GetDisplayUrl());
                rootURL = url.Scheme + Uri.SchemeDelimiter + url.Host;
            }

            return rootURL + (allowPublicAccess ? @"/public" : "") + @"/cms/files/?fn=" + WebUtility.UrlEncode(friendlyName);
        }
        #endregion

        #region public static string GetURL(string fileGUID, bool allowPublicAccess = false)
        /// <summary>
        /// Get url for a fileGUID
        /// </summary>
        public static string GetURL(string fileGUID, bool allowPublicAccess = false)
        {
            return (allowPublicAccess ? @"/public" : "") + @"/cms/files/?id=" + fileGUID;
        }
        #endregion

        #region public static string GetRootFileFolder()
        /// <summary>
        /// Get the root folder for files
        /// </summary>
        public static string GetRootFileFolder()
        {
            if (HttpHelper.Environment?.EnvironmentName == "Development")
            {
                return @"C:\Sites\Files";
            }

            return @"S:\Files";
        }
        #endregion

        #region public static void ReplaceRootFolder(CMSFile file)
        /// <summary>
        /// Replace the root folder in a file object, e.g. if the app is on secure-test but the file was added on a dev machine, or vice versa.
        /// </summary>
        public static void ReplaceRootFolder(CMSFile file)
        {
            string filePath = file.FilePath;
            string rootFolder = GetRootFileFolder();

            if (!filePath.StartsWith(rootFolder))
            {
                string endPath = filePath[(filePath.IndexOf(@"\Files", StringComparison.OrdinalIgnoreCase) + 6)..];
                string fixedPath = rootFolder + endPath;
                file.FilePath = fixedPath;
            }
        }
        #endregion

        #region public string FilePathToWebPath(string filePath)
        /// <summary>
        /// Remove root folder in the file path and change path separator to /
        /// </summary>
        public string FilePathToWebPath(string filePath)
        {
            return filePath.Replace(GetRootFileFolder(), "").Replace(@"\", @"/");
        }
        #endregion

        #region public bool CheckFilePermission(CMSFile file)
        public bool CheckFilePermission(CMSFile file)
        {
            AaudUser? currentUser = UserHelper.GetCurrentUser();

            if (_rapsContext! != null && currentUser != null)
            {
                if (file.AllowPublicAccess ||
                    (file.FileToPermissions.Count == 0 && UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure")) ||
                    (file.FileToPermissions.Count > 0 && file.FileToPermissions.Any(fp => UserHelper.GetAllPermissions(_rapsContext, currentUser).Any(p => string.Compare(fp.Permission, p.Permission, true) == 0))) ||
                    (file.FileToPeople.Count > 0 && file.FileToPeople.Any(fp => fp.IamId == currentUser.IamId)))
                {
                    return true;
                }

            }
            else if (file.AllowPublicAccess)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region public IActionResult DownloadZip(Controller controller, string[] fileGUIDs, string fileName = "FileDownload.zip")
        public IActionResult DownloadZip(Controller controller, string[] fileGUIDs, string fileName = "FileDownload.zip")
        {
            if (fileGUIDs.Length == 0 && fileName.Length == 0)
            {
                ArgumentNullException argumentNullException = new(nameof(fileGUIDs), "Missing fileGUIDs and file name parameters");
                throw argumentNullException;
            }

            //only allow good filename characters
            fileName = fileName.Replace(@"[^a-zA-Z0-9\.\-_ ]", "");
            List<CMSFile> files = new();
            AaudUser? currentUser = UserHelper.GetCurrentUser();

            foreach (var guid in fileGUIDs)
            {
                CMSFile? file = GetFile(guid, null, null, null, null);

                // only add files that exist and where the user has permission
                if (file != null && System.IO.File.Exists(file.FilePath) && CheckFilePermission(file))
                {
                    if (currentUser != null && _viperContext != null)
                    {
                        AuditFileAccess(_viperContext, file, currentUser, "AccessFile", string.Empty);
                    }

                    files.Add(file);
                }
            }

            // create a temp Zip file and populate it with the files
            string tempFileName = GetRootFileFolder() + @"\" + DateTime.Now.Ticks + fileName;

            using (FileStream fs = System.IO.File.Open(tempFileName, FileMode.OpenOrCreate))
            {
                using ZipArchive archive = new(fs, ZipArchiveMode.Update);
                foreach (var file in files)
                {
                    if (file.Encrypted && !string.IsNullOrEmpty(file.Key))
                    {
                        ZipArchiveEntry fileEntry = archive.CreateEntry(file.FriendlyName);
                        using StreamWriter writer = new(fileEntry.Open());
                        byte[] filebytes = System.IO.File.ReadAllBytes(file.FilePath);
                        filebytes = DecryptFile(filebytes, file.Key);

                        if (filebytes != null)
                        {
                            writer.BaseStream.Write(filebytes, 0, filebytes.Length);
                        }
                    }
                    else
                    {
                        archive.CreateEntryFromFile(file.FilePath, file.FriendlyName);
                    }

                }
            }

            // read the temp zip file then delete it
            byte[] bytes = System.IO.File.ReadAllBytes(tempFileName);
            if (bytes == null)
                return controller.NotFound();

            System.IO.File.Delete(tempFileName);

            string extension = "zip";

            return controller.File(bytes, MimeTypes[extension.ToLower()], fileName);

        }
        #endregion

        #region public IActionResult ProvideFile(Controller controller, string id, string friendlyName, string oldURL)
        public IActionResult ProvideFile(Controller controller, string id, string friendlyName, string oldURL)
        {
            AaudUser? currentUser = UserHelper.GetCurrentUser();

            if (id.Length == 0 && friendlyName.Length == 0 && oldURL.Length == 0)
            {
                ArgumentNullException argumentNullException = new(nameof(id), "Missing id, file name, and old name parameters");
                throw argumentNullException;
            }

            CMSFile? file = GetFile(id, oldURL, friendlyName, null, null);
            string detail = string.Empty;

            if (oldURL.Length > 0)
            {
                detail = "UsedOldURL#chr(13)##chr(10)#";
            }

            if (file == null)
            {
                LogFileNotFound(controller, id, friendlyName, oldURL, reason: "no-db-match");
                return controller.NotFound();
            }

            if (!System.IO.File.Exists(file.FilePath))
            {
                LogFileNotFound(controller, id, friendlyName, oldURL, reason: "missing-on-disk");
                return controller.NotFound();
            }

            if (!CheckFilePermission(file))
            {
                if (currentUser != null && _viperContext != null)
                {
                    AuditFileAccess(_viperContext, file, currentUser, "AccessFileDenied", detail);
                }

                controller.Response.StatusCode = 403;
                return controller.View("~/Views/Home/403.cshtml", (HttpStatusCode)403);
            }

            if (currentUser != null && _viperContext != null)
            {
                AuditFileAccess(_viperContext, file, currentUser, "AccessFile", detail);
            }

            byte[] bytes = System.IO.File.ReadAllBytes(file.FilePath);

            if (file.Encrypted && !string.IsNullOrEmpty(file.Key))
            {
                bytes = DecryptFile(bytes, file.Key);
            }

            string extension = file.FilePath[(file.FilePath.LastIndexOf('.') + 1)..];
            controller.Response.Headers["Content-Disposition"] = "inline; filename=" + friendlyName;

            return controller.File(bytes, MimeTypes[extension.ToLower()], true);

        }
        #endregion

        // VPR-143: [CMS-FILE-404] emits a warning whenever ProvideFile can't serve a file.
        // Grep the NLog output directory for the tag to see the distribution of misses
        // (legacy URLs, typos, bot probes, ACME challenges, files missing on disk).
        private void LogFileNotFound(Controller controller, string id, string friendlyName, string oldURL, string reason)
        {
            if (_logger is null)
            {
                return;
            }

            var request = controller.Request;
            _logger.LogWarning(
                "[CMS-FILE-404] reason={Reason} id={Id} friendlyName={FriendlyName} oldURL={OldUrl} " +
                "userAgent={UserAgent} referer={Referer} remoteIp={RemoteIp}",
                LogSanitizer.SanitizeString(reason),
                LogSanitizer.SanitizeString(id),
                LogSanitizer.SanitizeString(friendlyName),
                LogSanitizer.SanitizeString(oldURL),
                LogSanitizer.SanitizeString(request.Headers.UserAgent.ToString()),
                LogSanitizer.SanitizeString(request.Headers.Referer.ToString()),
                LogSanitizer.SanitizeString(request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty));
        }

        #region public static void AuditFileAccess(VIPERContext viperContext, CMSFile file, AaudUser user, string action, string detail)
        public static void AuditFileAccess(VIPERContext viperContext, CMSFile file, AaudUser user, string action, string detail)
        {
            UserHelper userHelper = new();

            FileAudit fileAudit = new()
            {
                Timestamp = DateTime.Now,
                Loginid = user.LoginId,
                Action = action,
                Detail = detail,
                FileGuid = file.FileGuid,
                FilePath = file.FilePath,
                IamId = user.IamId,
                FileMetaData = JsonSerializer.Serialize<CMSFileMetaData>(file.MetaData),
                ClientData = JsonSerializer.Serialize(userHelper.GetClientData())
            };

            viperContext.ChangeTracker.Clear();
            viperContext.Add(fileAudit);
            viperContext.SaveChanges();
        }
        #endregion

        #region public byte[] DecryptFile(byte[] encryptedData, string keystring)
        public byte[] DecryptFile(byte[] encryptedData, string keystring)
        {
            byte[] secretkey = GetSecretKey(keystring);

            Aes aes = Aes.Create();
            aes.Mode = CipherMode.ECB;

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(secretkey, null), CryptoStreamMode.Write))
            {
                cs.Write(encryptedData, 0, encryptedData.Length);
            }
            byte[] decryptedData = ms.ToArray();

            return decryptedData;

        }
        #endregion

        #region public string DecryptAES(string encryptedString, string Key)
        /// <summary>
        /// Required for Unix decoding FROM https://rextester.com/TGN19503
        /// </summary>
        /// <returns>decoded string</returns>
        public string DecryptAES(string encryptedString, string Key)
        {
            //First write to memory
            MemoryStream mmsStream = new();
            StreamWriter srwTemp = new(mmsStream);
            srwTemp.Write(encryptedString);
            srwTemp.Flush();
            mmsStream.Position = 0;

            MemoryStream outstream = new();

            //CallingUUDecode
            Codecs.UUDecode(mmsStream, outstream);

            //Extract the bytes of each of the values
            byte[] input = outstream.ToArray();
            byte[] key = Convert.FromBase64String(Key);

            string? decryptedText = null;

            using (Aes aes = Aes.Create())
            {
                // initialize settings to match those used by CF
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                aes.BlockSize = 128;
                aes.KeySize = 128;
                aes.Key = key;

                ICryptoTransform decryptor = aes.CreateDecryptor();

                using MemoryStream msDecrypt = new(input);
                using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new(csDecrypt);
                decryptedText = srDecrypt.ReadToEnd();
            }
            return decryptedText;
        }
        #endregion

        #region private byte[] getSecretKey(string key)
        private byte[] GetSecretKey(string key)
        {
            string keyFileFolder = @"S:\Settings\";

            if (HttpHelper.Environment?.EnvironmentName == "Development")
            {
                keyFileFolder = @"C:\Sites\Settings\";
            }
            string keyString = System.IO.File.ReadLines(keyFileFolder + "viperfiles.txt").Skip(1).Take(1).First();

            byte[] hiddenKey = Convert.FromBase64String(DecryptAES(key, keyString));
            return hiddenKey;
        }
        #endregion

    }

    public interface ICMS
    {
        IEnumerable<ContentBlock>? GetContentBlocksAllowed(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status);

        IEnumerable<ContentBlock>? GetContentBlocks(int? contentBlockID = null, string? friendlyName = null, string? system = null, string? viperSectionPath = null, string? page = null, int? blockOrder = null, bool? allowPublicAccess = null, int? status = null);

        CMSFile? GetFile(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name);

        CMSFile? GetFileByGuid(Guid fileGuid);

        CMSFile? GetFileByOldUrl(string oldUrl);

        CMSFile? GetFileByFriendlyName(string friendlyName);

        CMSFile? GetFileByFolderAndName(string folder, string name);

        IEnumerable<CMSFile> GetAllFiles(string? folder, bool? isPublic, string? search, string? status, bool? encrypted);

        static string GetFriendlyURL(string friendlyName, bool allowPublicAccess = false) => throw new NotImplementedException();

        static string GetURL(string fileGUID, bool allowPublicAccess = false) => throw new NotImplementedException();

        static string GetRootFileFolder() => throw new NotImplementedException();

        static void ReplaceRootFolder(CMSFile file) => throw new NotImplementedException();

        string FilePathToWebPath(string filePath);

        bool CheckFilePermission(CMSFile file);

        IActionResult DownloadZip(Controller controller, string[] fileGUIDs, string fileName = "FileDownload.zip");

        IActionResult ProvideFile(Controller controller, string id, string friendlyName, string oldURL);

        static void AuditFileAccess(VIPERContext viperContext, CMSFile file, AaudUser user, string action, string detail) => throw new NotImplementedException();

        byte[] DecryptFile(byte[] encryptedData, string keystring);

        string DecryptAES(string encryptedString, string Key);

    }
}
