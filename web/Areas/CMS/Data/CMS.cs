using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OWASP.AntiSamy.Html;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using Viper.Areas.CMS.Models;
using Viper.Classes.SQLContext;
using Viper.Models;
using Viper.Models.AAUD;
using Viper.Models.VIPER;
using static System.Net.Mime.MediaTypeNames;

namespace Viper.Areas.CMS.Data
{
    public class CMS : ICMS
    {
        #region Properties (private/public)
        private readonly VIPERContext? _viperContext;
        private readonly RAPSContext? _rapsContext;

        public IUserHelper UserHelper;
        public Dictionary<string, string> MimeTypes = new()
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
        public CMS()
        {
            this._viperContext = (VIPERContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(VIPERContext));
            this._rapsContext = (RAPSContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(RAPSContext));
            UserHelper = new UserHelper();
        }

        public CMS(VIPERContext viperContext, RAPSContext rapsContext)
        {
            this._viperContext = viperContext;
            this._rapsContext = rapsContext;
            UserHelper = new UserHelper();
        }
        #endregion

        #region public IEnumerable<ContentBlock>? GetContentBlocksAllowed(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status)
        /// <summary>
        /// Get content blocks and filter based on permissions
        /// </summary>
        /// <param name="contentBlockID"></param>
        /// <param name="friendlyName"></param>
        /// <param name="system"></param>
        /// <param name="viperSectionPath"></param>
        /// <param name="page"></param>
        /// <param name="blockOrder"></param>
        /// <param name="allowPublicAccess"></param>
        /// <param name="status"></param>
        /// <returns>List of blocks</returns>
        public IEnumerable<ContentBlock>? GetContentBlocksAllowed(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status)
        {
            // get blocks based on paramenters
            var blocks = GetContentBlocks(contentBlockID, friendlyName, system, viperSectionPath, page, blockOrder, allowPublicAccess, status);

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

                if(goodBlocks.Any())
                {
                    Sanitize(goodBlocks);
                }
                return goodBlocks;

            }
            else
            { return null; }
        }
        #endregion

        #region public IEnumerable<ContentBlock>? GetContentBlocks(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status)
        /// <summary>
        /// Get content blocks without filtering on permissions
        /// </summary>
        /// <param name="contentBlockId"></param>
        /// <param name="friendlyName"></param>
        /// <param name="system"></param>
        /// <param name="viperSectionPath"></param>
        /// <param name="page"></param>
        /// <param name="blockOrder"></param>
        /// <param name="allowPublicAccess"></param>
        /// <param name="status"></param>
        /// <returns>List of blocks</returns>
        public IEnumerable<ContentBlock>? GetContentBlocks(int? contentBlockId = null, string? friendlyName = null, string? system = null,
            string? viperSectionPath = null, string? page = null, int? blockOrder = null,
            bool? allowPublicAccess = null, int? status = null)
        {
            // get blocks based on paramenters
            var blocks = _viperContext?.ContentBlocks
                    .Include(p => p.ContentBlockToPermissions)
                    .Include(f => f.ContentBlockToFiles)
                        .ThenInclude(cbf => cbf.File)
                    .Include(h => h.ContentHistories)
                    .Where(c => c.ContentBlockId.Equals(contentBlockId) || contentBlockId == null)
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

            if (blocks != null)
            {
                Sanitize(blocks);
                Policy policy = Policy.GetInstance("antisamy-cms.xml");
                var antiSamy = new AntiSamy();

                foreach (var b in blocks)
                {
                    // sanitize content
                    CleanResults results = antiSamy.Scan(b.Content, policy);
                    b.Content = results.GetCleanHtml();

                }

                return blocks;

            }
            else
            { return null; }
        }
        #endregion

        public void Sanitize(IEnumerable<ContentBlock> blocks)
        {
            Policy policy = Policy.GetInstance("antisamy-cms.xml");
            var antiSamy = new AntiSamy();

            foreach (var b in blocks)
            {
                // sanitize content
                CleanResults results = antiSamy.Scan(b.Content, policy);
                b.Content = results.GetCleanHtml();
            }
        }

        #region public CMSFile? GetFile(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name)
        /// <summary>
        /// Returns the first file that matches the parameters past (or null)
        /// </summary>
        /// <param name="fileGUID"></param>
        /// <param name="oldURL"></param>
        /// <param name="friendlyName"></param>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        /// <param name="getDeleted"></param>
        /// <returns>File object</returns>
        public CMSFile? GetFile(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name)
        {
            var files = GetFiles(fileGUID, oldURL, friendlyName, folder, name);

            return files?.FirstOrDefault();

        }
        #endregion

        #region public IEnumerable<CMSFile> GetFiles(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name)
        /// <summary>
        /// Returns all files that match the given parameters
        /// </summary>
        /// <param name="fileGUID"></param>
        /// <param name="oldURL"></param>
        /// <param name="friendlyName"></param>
        /// <param name="folder">specify folder to return files in that folder (e.g. cats, students, sosa)</param>
        /// <param name="name"></param>
        /// <param name="getDeleted"></param>
        /// <returns>List of file objects</returns>
        /// <exception cref="FileNotFoundException"></exception>
        public IEnumerable<CMSFile> GetFiles(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name)
        {
            if (fileGUID == null && oldURL == null && friendlyName == null && folder == null && name == null)
            {
                throw new FileNotFoundException();
            }

            // get files based on paramenters
            var files = _viperContext?.Files
                    .Include(p => p.FileToPermissions)
                    .Include(p => p.FileToPeople)
                    .Where(f => f.FileGuid.ToString().Equals(fileGUID) || string.IsNullOrEmpty(fileGUID))
                    .Where(f => string.IsNullOrEmpty(f.OldUrl) ? string.IsNullOrEmpty(oldURL) : f.OldUrl.Equals(oldURL) || string.IsNullOrEmpty(oldURL))
                    .Where(f => f.FriendlyName.Equals(friendlyName) || string.IsNullOrEmpty(friendlyName))
                    .Where(f => f.FilePath.Equals(folder + @"\" + name) || (string.IsNullOrEmpty(folder) && string.IsNullOrEmpty(name)))
                .OrderBy(c => c.FilePath)
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
            else
            {
                return new List<CMSFile>();
            }

        }
        #endregion

        #region public IEnumerable<CMSFile> GetAllFiles(string? folder, bool? isPublic, string? search, string? status, bool? encrypted)
        /// <summary>
        /// Search for matching files
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="isPublic"></param>
        /// <param name="search"></param>
        /// <param name="status"></param>
        /// <param name="encrypted"></param>
        /// <returns></returns>
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
            else
            {
                return new List<CMSFile>();
            }

        }
        #endregion

        #region public static string GetFriendlyURL(string friendlyName, bool allowPublicAccess = false)
        /// <summary>
        /// Get Friendly URL for a friendly name. Currently, always points to ColdFusion Viper
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <param name="allowPublicAccess"></param>
        /// <returns></returns>
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
        /// <param name="fileGUID"></param>
        /// <param name="allowPublicAccess"></param>
        /// <returns></returns>
        public static string GetURL(string fileGUID, bool allowPublicAccess = false)
        {
            return (allowPublicAccess ? @"/public" : "") + @"/cms/files/?id=" + fileGUID;
        }
        #endregion

        #region public static string GetRootFileFolder()
        /// <summary>
        /// Get the root folder for files
        /// </summary>
        /// <returns></returns>
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
        /// <param name="file"></param>
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
        /// <param name="filePath"></param>
        /// <returns></returns>
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
            string tempFileName = CMS.GetRootFileFolder() + @"\" + DateTime.Now.Ticks + fileName;

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
                return controller.NotFound();
            }
            else if (!System.IO.File.Exists(file.FilePath))
            {
                return controller.NotFound();
            }
            else if (!CheckFilePermission(file))
            {
                if (currentUser != null && _viperContext != null)
                {
                    AuditFileAccess(_viperContext, file, currentUser, "AccessFileDenied", detail);
                }

                controller.Response.StatusCode = 403;
                return controller.View("~/Views/Home/403.cshtml", (HttpStatusCode)403);
            }
            else
            {
                if (currentUser != null && _viperContext != null)
                {
                    AuditFileAccess(_viperContext, file, currentUser, "AccessFile", detail);
                }

                byte[] bytes = System.IO.File.ReadAllBytes(file.FilePath);

                if (file.Encrypted && !string.IsNullOrEmpty(file.Key))
                {
                    bytes = DecryptFile(bytes, file.Key);
                }

                if (bytes == null)
                    return controller.NotFound();

                string extension = file.FilePath[(file.FilePath.LastIndexOf('.') + 1)..];
                controller.Response.Headers["Content-Disposition"] = "inline; filename=" + friendlyName;

                return controller.File(bytes, MimeTypes[extension.ToLower()], true);
            }

        }
        #endregion

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
                ClientData = JsonSerializer.Serialize<ClientData>(userHelper.GetClientData())
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
        /// <param name="encryptedString"></param>
        /// <param name="Key"></param>
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

        IEnumerable<ContentBlock>? GetContentBlocks(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status);

        CMSFile? GetFile(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name);

        IEnumerable<CMSFile> GetFiles(string? fileGUID, string? oldURL, string? friendlyName, string? folder, string? name);

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
