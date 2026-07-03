using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models;
using Viper.Models.VIPER;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    /// <summary>
    /// File management API (list/upload/edit/delete/restore/audit). Downloads are served by
    /// the existing permission-checked CMSController (/CMS/Files), which legacy URLs point at.
    /// </summary>
    [Route("/api/cms/files")]
    [Permission(Allow = CmsPermissions.AllFiles)]
    public class CMSFilesController : ApiController
    {
        private const long MaxUploadBytes = 100_000_000;

        // Import/preview/bulk-encrypt process items sequentially (file + DB work per item), so an
        // oversized batch risks blowing through the request timeout.
        private const int MaxBatchItems = 500;

        private readonly ICmsFileService _fileService;
        private readonly ICmsFileStorageService _storage;
        private readonly ICmsFileAuditService _auditService;
        private readonly ICmsFileImportService _importService;
        private readonly RAPSContext _rapsContext;
        private readonly ILogger<CMSFilesController> _logger;
        private readonly IUserHelper _userHelper;

        public CMSFilesController(ICmsFileService fileService, ICmsFileStorageService storage,
            ICmsFileAuditService auditService, ICmsFileImportService importService, RAPSContext rapsContext,
            ILogger<CMSFilesController> logger, IUserHelper userHelper)
        {
            _fileService = fileService;
            _storage = storage;
            _auditService = auditService;
            _importService = importService;
            _rapsContext = rapsContext;
            _logger = logger;
            _userHelper = userHelper;
        }

        // GET /api/cms/files
        [HttpGet]
        [ApiPagination(DefaultPerPage = 50, MaxPerPage = 500)]
        public async Task<ActionResult<List<CmsFileDto>>> GetFiles(
            string? folder,
            string? search,
            bool? encrypted,
            bool? isPublic,
            ApiPagination? pagination,
            string status = "active",
            string? sortBy = "friendlyName",
            bool descending = false,
            CancellationToken ct = default)
        {
            var page = pagination?.Page ?? 1;
            var perPage = pagination?.PerPage ?? 50;
            var (files, total) = await _fileService.GetFilesAsync(folder, status, search, encrypted, isPublic,
                page, perPage, sortBy, descending, OwnerRestriction(), ct);
            if (pagination != null)
            {
                pagination.TotalRecords = total;
            }
            return files;
        }

        // GET /api/cms/files/folders
        // includeData=true unions in folders that exist only on file records (for
        // filtering); the default disk-backed list is the upload destination allow-list.
        [HttpGet("folders")]
        public async Task<ActionResult<List<string>>> GetFolders(bool includeData = false, CancellationToken ct = default)
        {
            return includeData ? await _storage.GetFilterFoldersAsync(ct) : _storage.GetTopLevelFolders();
        }

        // GET /api/cms/files/folder-counts
        // File counts per top-level folder for filter dropdowns; same status/encrypted/isPublic
        // semantics as the list endpoint.
        [HttpGet("folder-counts")]
        public async Task<ActionResult<List<CmsFolderCountDto>>> GetFolderCounts(
            bool? encrypted, bool? isPublic, string status = "active", CancellationToken ct = default)
        {
            return await _fileService.GetFolderCountsAsync(status, encrypted, isPublic, OwnerRestriction(), ct);
        }

        // GET /api/cms/files/audit
        [HttpGet("audit")]
        [ApiPagination(DefaultPerPage = 50, MaxPerPage = 500)]
        public async Task<ActionResult<List<FileAudit>>> GetAudit(
            Guid? fileGuid,
            string? action,
            string? loginId,
            DateTime? from,
            DateTime? to,
            string? search,
            ApiPagination? pagination,
            CancellationToken ct = default)
        {
            var filter = new CmsFileAuditFilter
            {
                FileGuid = fileGuid,
                Action = action,
                LoginId = loginId,
                From = from,
                To = to,
                Search = search
            };
            var page = pagination?.Page ?? 1;
            var perPage = pagination?.PerPage ?? 50;
            var entries = await _auditService.GetAuditEntriesAsync(filter, page, perPage, ct);
            if (pagination != null)
            {
                pagination.TotalRecords = await _auditService.GetAuditEntryCountAsync(filter, ct);
            }
            return entries;
        }

        // GET /api/cms/files/{fileGuid}
        [HttpGet("{fileGuid:guid}")]
        public async Task<ActionResult<CmsFileDto>> GetFile(Guid fileGuid, CancellationToken ct = default)
        {
            var file = await _fileService.GetFileAsync(fileGuid, ct);
            if (file == null)
            {
                return NotFound();
            }
            return file;
        }

        // GET /api/cms/files/check-name — pre-upload conflict check for a folder + file name
        [HttpGet("check-name")]
        public async Task<ActionResult<CmsFileNameCheckDto>> CheckName(string folder, string fileName,
            CancellationToken ct = default)
        {
            try
            {
                return await _fileService.CheckNameAsync(folder, fileName, ct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST /api/cms/files
        [HttpPost]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<ActionResult<CmsFileDto>> UploadFile([FromForm] CmsFileCreateRequest request, IFormFile? file,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("A file is required.");
            }

            try
            {
                return await _fileService.CreateFileAsync(request, file, ct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                LogFileConflict(nameof(UploadFile), request.Folder, file.FileName, ex);
                return BadRequest(ex.Message);
            }
        }

        // PUT /api/cms/files/{fileGuid}
        [HttpPut("{fileGuid:guid}")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<ActionResult<CmsFileDto>> UpdateFile(Guid fileGuid, [FromForm] CmsFileUpdateRequest request,
            IFormFile? file, CancellationToken ct = default)
        {
            try
            {
                var updated = await _fileService.UpdateFileAsync(fileGuid, request, file, ct);
                if (updated == null)
                {
                    return NotFound();
                }
                return updated;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            // Before InvalidOperationException: CmsConcurrencyException derives from it, and a
            // stale edit must surface as 409 (someone saved first), not a generic 400.
            catch (CmsConcurrencyException ex)
            {
                return Conflict(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                LogFileConflict(nameof(UpdateFile), null, file?.FileName, ex);
                return BadRequest(ex.Message);
            }
        }

        // DELETE /api/cms/files/{fileGuid}?permanent=true|false
        [HttpDelete("{fileGuid:guid}")]
        public async Task<IActionResult> DeleteFile(Guid fileGuid, bool permanent = false, CancellationToken ct = default)
        {
            if (permanent)
            {
                // Hard delete is restricted to admins; soft delete is available to all file managers.
                if (!IsCmsAdmin())
                {
                    return ForbidApi();
                }
                return await _fileService.PermanentlyDeleteFileAsync(fileGuid, ct) ? NoContent() : NotFound();
            }
            return await _fileService.SoftDeleteFileAsync(fileGuid, ct) ? NoContent() : NotFound();
        }

        // POST /api/cms/files/{fileGuid}/restore
        [HttpPost("{fileGuid:guid}/restore")]
        public async Task<IActionResult> RestoreFile(Guid fileGuid, CancellationToken ct = default)
        {
            if (!IsCmsAdmin())
            {
                // Non-admins may only restore files they trashed themselves.
                var file = await _fileService.GetFileAsync(fileGuid, ct);
                var login = _userHelper.GetCurrentUser()?.LoginId;
                if (file?.DeletedOn == null || login == null
                    || !string.Equals(file.ModifiedBy, login, StringComparison.OrdinalIgnoreCase))
                {
                    return ForbidApi();
                }
            }
            return await _fileService.RestoreFileAsync(fileGuid, ct) ? NoContent() : NotFound();
        }

        private bool IsCmsAdmin() =>
            _userHelper.HasPermission(_rapsContext, _userHelper.GetCurrentUser(), CmsPermissions.Admin);

        // Scopes trash queries: admins see the whole trash (null); everyone else only the files they
        // deleted (matched by ModifiedBy), so nothing leaks across users but people can recover their own.
        // A missing user context fails closed (empty string matches no ModifiedBy) rather than falling
        // through to the admin-level unrestricted view.
        private string? OwnerRestriction()
        {
            var user = _userHelper.GetCurrentUser();
            if (user == null)
            {
                return string.Empty;
            }
            return IsCmsAdmin() ? null : user.LoginId;
        }

        // POST /api/cms/files/import — move files from the legacy VIPER webroot into the store
        [HttpPost("import")]
        public async Task<ActionResult<List<CmsFileImportResult>>> ImportFiles(CmsFileImportRequest request,
            CancellationToken ct = default)
        {
            if (request.FilePaths.Count == 0)
            {
                return BadRequest("At least one file path is required.");
            }
            if (request.FilePaths.Count > MaxBatchItems)
            {
                return BadRequest($"A single request may include at most {MaxBatchItems} items.");
            }

            try
            {
                return await _importService.ImportFilesAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST /api/cms/files/import/preview — dry-run validation of an import request
        [HttpPost("import/preview")]
        public async Task<ActionResult<List<CmsFileImportPreviewResult>>> PreviewImport(CmsFileImportRequest request,
            CancellationToken ct = default)
        {
            if (request.FilePaths.Count == 0)
            {
                return BadRequest("At least one file path is required.");
            }
            if (request.FilePaths.Count > MaxBatchItems)
            {
                return BadRequest($"A single request may include at most {MaxBatchItems} items.");
            }

            try
            {
                return await _importService.PreviewImportAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST /api/cms/files/bulk-encrypt
        [HttpPost("bulk-encrypt")]
        public async Task<ActionResult<List<CmsBulkEncryptResult>>> BulkEncrypt(List<Guid> fileGuids,
            CancellationToken ct = default)
        {
            if (fileGuids.Count == 0)
            {
                return BadRequest("At least one file is required.");
            }
            if (fileGuids.Count > MaxBatchItems)
            {
                return BadRequest($"A single request may include at most {MaxBatchItems} items.");
            }
            return await _importService.BulkEncryptAsync(fileGuids, ct);
        }

        private void LogFileConflict(string operation, string? folder, string? fileName, Exception ex)
        {
            _logger.LogWarning(ex, "CMS file {Operation} conflict folder={Folder} fileName={FileName}",
                LogSanitizer.SanitizeString(operation),
                LogSanitizer.SanitizeString(folder),
                LogSanitizer.SanitizeString(fileName));
        }
    }
}
