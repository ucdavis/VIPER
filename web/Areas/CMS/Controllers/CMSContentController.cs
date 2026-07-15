using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models;
using Viper.Services;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    //NOTE: no controller-wide permissions being checked because CMS content can be publically accessible
    [Route("/api/CMS/content")]
    public class CMSContentController : ApiController
    {
        // Uploads through the content-scoped file endpoint match CMSFilesController's ceiling.
        private const long MaxUploadBytes = 100_000_000;

        private readonly VIPERContext _context;
        private readonly RAPSContext _rapsContext;
        private readonly IHtmlSanitizerService _sanitizerService;
        private readonly ICmsContentBlockService _blockService;
        private readonly ICmsFileService _fileService;
        private readonly ICmsFileStorageService _storage;
        private readonly IUserHelper _userHelper;
        private readonly ILogger<CMSContentController> _logger;

        public CMSContentController(VIPERContext context, RAPSContext rapsContext,
            IHtmlSanitizerService sanitizerService, ICmsContentBlockService blockService,
            ICmsFileService fileService, ICmsFileStorageService storage, IUserHelper userHelper,
            ILogger<CMSContentController> logger)
        {
            _context = context;
            _rapsContext = rapsContext;
            _sanitizerService = sanitizerService;
            _blockService = blockService;
            _fileService = fileService;
            _storage = storage;
            _userHelper = userHelper;
            _logger = logger;
        }

        //GET: content
        [HttpGet]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [ApiPagination(DefaultPerPage = 50, MaxPerPage = 500)]
        public async Task<ActionResult<List<ContentBlockDto>>> GetContentBlocks(
            ApiPagination? pagination,
            string status = "active",
            string? system = null,
            string? viperSectionPath = null,
            string? search = null,
            bool? isPublic = null,
            string? sortBy = "title",
            bool descending = false,
            CancellationToken ct = default)
        {
            var page = pagination?.Page ?? 1;
            var perPage = pagination?.PerPage ?? 50;
            var (blocks, total) = await _blockService.GetContentBlocksAsync(status, system, viperSectionPath, search,
                isPublic, page, perPage, sortBy, descending, ct);
            if (pagination != null)
            {
                pagination.TotalRecords = total;
            }
            return blocks;
        }

        //GET: content/section-paths
        [HttpGet("section-paths")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<List<string>>> GetViperSectionPaths(CancellationToken ct = default)
        {
            return await _blockService.GetViperSectionPathsAsync(ct);
        }

        // GET: content/folders
        // The section path IS a file folder (legacy parity): a block's files live in this folder.
        // Exposed to content-block editors so the section path can be chosen from the real upload
        // folders without requiring AllFiles (the /api/cms/files/folders endpoint is AllFiles-gated).
        [HttpGet("folders")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks + "," + CmsPermissions.CreateContentBlock)]
        public ActionResult<List<string>> GetFolders()
        {
            return _storage.GetTopLevelFolders();
        }

        //GET: content/5 — the editor loads a single block. Widened to any SVMSecure user so
        //delegated editors can open blocks they hold an edit permission for; CanEditAsync is the
        //real boundary (managers always pass, delegates only for their live blocks).
        [HttpGet("{contentBlockId:int}")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<ContentBlockDto>> GetContentBlock(int contentBlockId, CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            var block = await _blockService.GetContentBlockAsync(contentBlockId, ct);
            if (block == null)
            {
                // Guards the narrow race where the block is removed between the checks above and here.
                return NotFound();
            }
            return block;
        }

        //GET: content/fn/{friendlyName} — public display endpoint; permission filtering happens
        //inside GetContentBlocksAllowed (public flag, block permissions, or CMS admin).
        [HttpGet("fn/{friendlyName}")]
        public ActionResult<PublicContentBlockDto> GetContentBlockByFn(string friendlyName)
        {
            // status: 1 = active only. A public display endpoint must never serve soft-deleted
            // blocks (passing null would include DeletedOn != null rows).
            var blocks = new Data.CMS(_context, _rapsContext, _sanitizerService)
                .GetContentBlocksAllowed(null, friendlyName, null, null, null, null, null, 1)?.ToList();
            if (blocks == null || blocks.Count == 0)
            {
                return NotFound();
            }

            // Project to the public DTO so this anonymous endpoint never serializes the raw entity
            // graph (attached-file encryption Keys, server FilePaths, unsanitized ContentHistory,
            // permission rows) nor management metadata (editor login ids, permission names,
            // System/section placement). Content is already render-sanitized by
            // GetContentBlocksAllowed; display consumers read only content + title.
            return CmsContentBlockMapper.ToPublicDto(blocks[0]);
        }

        //GET: content/5/history — widened to editors (delegates may view/load history for their
        //blocks); CanEditAsync gates access. The cross-block history viewer below stays manager-only.
        [HttpGet("{contentBlockId:int}/history")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<List<ContentHistoryListItemDto>>> GetHistory(int contentBlockId, CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            return await _blockService.GetHistoryAsync(contentBlockId, ct);
        }

        //GET: content/5/history/12
        [HttpGet("{contentBlockId:int}/history/{contentHistoryId:int}")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<ContentHistoryDto>> GetHistoryVersion(int contentBlockId, int contentHistoryId,
            CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            var version = await _blockService.GetHistoryVersionAsync(contentBlockId, contentHistoryId, ct);
            if (version == null)
            {
                return NotFound();
            }
            return version;
        }

        //GET: content/5/history/12/diff — rendered diff of this version against the previous version
        [HttpGet("{contentBlockId:int}/history/{contentHistoryId:int}/diff")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<ContentHistoryDiffDto>> GetHistoryVersionDiff(int contentBlockId, int contentHistoryId,
            CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            var diff = await _blockService.GetHistoryVersionDiffAsync(contentBlockId, contentHistoryId, ct);
            if (diff == null)
            {
                return NotFound();
            }
            return diff;
        }

        //POST: content/5/history/12/diff — rendered diff of a posted draft against this version.
        //POST (not GET) because the editor's current content rides in the body.
        [HttpPost("{contentBlockId:int}/history/{contentHistoryId:int}/diff")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<ContentHistoryDiffDto>> DiffAgainstHistoryVersion(int contentBlockId, int contentHistoryId,
            DiffAgainstHistoryRequest request, CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            var diff = await _blockService.DiffContentAgainstHistoryAsync(contentBlockId, contentHistoryId, request.Content, ct);
            if (diff == null)
            {
                return NotFound();
            }
            return diff;
        }

        //GET: content/history — cross-block edit-history viewer (the literal segment does not
        //collide with the int-constrained {contentBlockId}/history route).
        [HttpGet("history")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        [ApiPagination(DefaultPerPage = 50, MaxPerPage = 500)]
        public async Task<ActionResult<List<ContentHistoryAuditDto>>> GetHistoryEntries(
            int? contentBlockId,
            string? modifiedBy,
            DateTime? from,
            DateTime? to,
            string? search,
            ApiPagination? pagination,
            CancellationToken ct = default)
        {
            var filter = new CmsContentHistoryFilter
            {
                ContentBlockId = contentBlockId,
                ModifiedBy = modifiedBy,
                From = from,
                To = to,
                Search = search
            };
            var page = pagination?.Page ?? 1;
            var perPage = pagination?.PerPage ?? 50;
            var entries = await _blockService.GetHistoryEntriesAsync(filter, page, perPage, ct);
            if (pagination != null)
            {
                pagination.TotalRecords = await _blockService.GetHistoryEntryCountAsync(filter, ct);
            }
            return entries;
        }

        //POST: content
        [HttpPost]
        [Permission(Allow = CmsPermissions.CreateContentBlock + "," + CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<ContentBlockDto>> CreateContentBlock(CMSBlockAddEdit block, CancellationToken ct = default)
        {
            string inputCheck = CheckBlockForRequiredFields(block);
            if (!string.IsNullOrEmpty(inputCheck))
            {
                return BadRequest(inputCheck);
            }

            try
            {
                return await _blockService.CreateContentBlockAsync(block, ct);
            }
            catch (ArgumentException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }

        //PUT: content/5
        [HttpPut("{contentBlockId:int}")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<ContentBlockDto>> UpdateContentBlock(int contentBlockId, CMSBlockAddEdit block,
            CancellationToken ct = default)
        {
            if (contentBlockId != block.ContentBlockId)
            {
                return BadRequest();
            }

            string inputCheck = CheckBlockForRequiredFields(block);
            if (!string.IsNullOrEmpty(inputCheck))
            {
                return BadRequest(inputCheck);
            }

            try
            {
                var updated = await _blockService.UpdateContentBlockAsync(contentBlockId, block, ct);
                if (updated == null)
                {
                    return NotFound();
                }
                return updated;
            }
            catch (CmsConcurrencyException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }

        //PATCH: content/5/content — quick save for content-only edits (content + optional attached
        //files). Widened to editors; CanEditAsync is the field-whitelisting boundary — this path never
        //maps title, placement, or permission fields, so a delegate cannot alter them here.
        [HttpPatch("{contentBlockId:int}/content")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<ContentBlockDto>> UpdateContentOnly(int contentBlockId,
            ContentOnlyUpdate update, CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            try
            {
                var updated = await _blockService.UpdateContentOnlyAsync(contentBlockId, update.Content,
                    update.LastModifiedOn, update.FileGuids, ct);
                if (updated == null)
                {
                    return NotFound();
                }
                return updated;
            }
            catch (CmsConcurrencyException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }

        //GET: content/editable — blocks the current user may edit by delegation (their permissions
        //intersect the block's edit list). Managers use the full list page; this returns delegated
        //matches only. Literal segment does not collide with the int-constrained {contentBlockId} route.
        [HttpGet("editable")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<List<ContentBlockDto>>> GetEditableBlocks(CancellationToken ct = default)
        {
            return await _blockService.GetEditableBlocksAsync(ct);
        }

        //GET: content/attachable-files?search=&contentBlockId= — attach-by-search picker for the editor.
        //In edit mode the caller passes the block id and must be able to edit it. In create mode there
        //is no block yet, so only block creators/managers may search the store.
        [HttpGet("attachable-files")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<List<CmsAttachableFileDto>>> SearchAttachableFiles(
            string? search, int? contentBlockId, CancellationToken ct = default)
        {
            if (contentBlockId == null)
            {
                var user = _userHelper.GetCurrentUser();
                bool canCreate = _userHelper.HasPermission(_rapsContext, user, CmsPermissions.ManageContentBlocks)
                    || _userHelper.HasPermission(_rapsContext, user, CmsPermissions.CreateContentBlock);
                if (!canCreate)
                {
                    return ForbidApi();
                }
            }
            else if (!await _blockService.CanEditAsync(contentBlockId.Value, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId.Value, ct);
            }
            return await _blockService.SearchAttachableFilesAsync(search, ct);
        }

        //POST: content/5/restore
        [HttpPost("{contentBlockId:int}/restore")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<IActionResult> RestoreContentBlock(int contentBlockId, CancellationToken ct = default)
        {
            return await _blockService.RestoreAsync(contentBlockId, ct) ? NoContent() : NotFound();
        }

        //DELETE: content/5?permanent=true|false
        [HttpDelete("{contentBlockId:int}")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<IActionResult> DeleteContentBlock(int contentBlockId, bool permanent = false,
            CancellationToken ct = default)
        {
            if (permanent)
            {
                // Permanent delete removes history and cannot be undone; admin only, matching the
                // legacy "dev only" (SVMSecure.CATS.Admin) gate on permanent content-block delete.
                if (!_userHelper.HasPermission(_rapsContext, _userHelper.GetCurrentUser(), CmsPermissions.Admin))
                {
                    return ForbidApi();
                }
                return await _blockService.PermanentlyDeleteAsync(contentBlockId, ct) ? NoContent() : NotFound();
            }
            return await _blockService.SoftDeleteAsync(contentBlockId, ct) ? NoContent() : NotFound();
        }

        // GET: content/5/files/check-name?fileName= — pre-upload conflict check scoped to the block's
        // folder (its section path). Lets delegated editors, who lack AllFiles, use the same
        // conflict-check flow as managers without exposing the folder-wide file endpoint.
        [HttpGet("{contentBlockId:int}/files/check-name")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<CmsFileNameCheckDto>> CheckBlockFileName(int contentBlockId, string fileName,
            CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            // Lightweight lookup: this runs once per staged file, so don't load (and sanitize)
            // the whole block when only the section path is needed.
            var (found, sectionPath) = await _blockService.GetSectionPathAsync(contentBlockId, ct);
            if (!found)
            {
                return NotFound();
            }
            // The section path IS the upload folder; without one there is nowhere to check names.
            // 400 (not 404) to match UploadBlockFile: the block exists but isn't set up for uploads.
            if (string.IsNullOrEmpty(sectionPath))
            {
                return BadRequest("The content block has no VIPER section path to upload into.");
            }
            try
            {
                return await _fileService.CheckNameAsync(sectionPath, fileName, ct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: content/5/files — inline upload from the editor. The destination folder, public flag,
        // and permissions are taken from the block (a delegate cannot choose them); only the file,
        // its name, and the overwrite flag come from the client. One code path for managers and
        // delegates alike.
        [HttpPost("{contentBlockId:int}/files")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<CmsFileDto>> UploadBlockFile(int contentBlockId,
            [FromForm] ContentBlockFileUpload form, IFormFile? file, CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }
            if (file == null || file.Length == 0)
            {
                return BadRequest("A file is required.");
            }
            var block = await _blockService.GetContentBlockAsync(contentBlockId, ct);
            if (block == null)
            {
                return NotFound();
            }
            // The section path IS the upload folder; refuse the upload when the block has none
            // (mirrors CheckBlockFileName) rather than falling back to the storage root.
            if (string.IsNullOrEmpty(block.ViperSectionPath))
            {
                return BadRequest("The content block has no VIPER section path to upload into.");
            }

            var request = new CmsFileCreateRequest
            {
                Folder = block.ViperSectionPath,
                AllowPublicAccess = block.AllowPublicAccess,
                Permissions = block.Permissions,
                FileName = form.FileName,
                Overwrite = form.Overwrite
            };
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
                LogFileConflict(nameof(UploadBlockFile), request.Folder, file.FileName, ex);
                return BadRequest(ex.Message);
            }
        }

        // DELETE: content/5/files/{fileGuid} — rollback of an inline upload only. A file may be
        // soft-deleted through the editor solely when the current user uploaded it (ModifiedBy), it
        // lives in this block's folder, and no OTHER block attaches it. Anything else is a managed
        // file that must be removed from the file-management UI, so it is refused here.
        [HttpDelete("{contentBlockId:int}/files/{fileGuid:guid}")]
        [Permission(Allow = "SVMSecure")]
        public async Task<IActionResult> DeleteBlockFile(int contentBlockId, Guid fileGuid, CancellationToken ct = default)
        {
            if (!await _blockService.CanEditAsync(contentBlockId, ct))
            {
                return await ForbiddenOrNotFoundAsync(contentBlockId, ct);
            }

            // The block-folder / uploader / not-shared-elsewhere rules live in the service (it owns
            // the DB access); null means the file does not exist, false means it is not an eligible
            // rollback.
            var eligible = await _blockService.IsFileRollbackDeletableAsync(contentBlockId, fileGuid, ct);
            if (eligible == null)
            {
                return NotFound();
            }
            if (!eligible.Value)
            {
                return ForbidApi();
            }

            // Recheck not-shared-elsewhere and soft-delete together (see RollbackDeleteFileAsync) so a
            // block that attaches the file after the eligibility check above can't have its attachment
            // deleted out from under it. A false result means it became shared/gone in that window.
            return await _fileService.RollbackDeleteFileAsync(fileGuid, contentBlockId, ct)
                ? NoContent()
                : Conflict("The file could not be rolled back; it may now be attached to another block.");
        }

        private void LogFileConflict(string operation, string? folder, string? fileName, Exception ex)
        {
            _logger.LogWarning(ex, "CMS content-scoped file {Operation} conflict folder={Folder} fileName={FileName}",
                LogSanitizer.SanitizeString(operation),
                LogSanitizer.SanitizeString(folder),
                LogSanitizer.SanitizeString(fileName));
        }

        // A block-scoped request the current user cannot edit: 404 when the block does not exist at
        // all, 403 when it exists but is not theirs to edit. Keeping "exists but forbidden" a 403
        // preserves the delegated "edit access was revoked" signal the editor relies on; a genuinely
        // bad id gets a true 404. GetSectionPathAsync is a light existence probe (no content load).
        private async Task<ActionResult> ForbiddenOrNotFoundAsync(int contentBlockId, CancellationToken ct)
        {
            var (found, _) = await _blockService.GetSectionPathAsync(contentBlockId, ct);
            return found ? ForbidApi() : NotFound();
        }

        private static string CheckBlockForRequiredFields(CMSBlockAddEdit userInput)
        {
            string errors = "";
            if (string.IsNullOrEmpty(userInput.Title))
            {
                errors += "Title is required. ";
            }
            if (string.IsNullOrEmpty(userInput.System))
            {
                errors += "System is required. ";
            }
            return errors;
        }
    }

    public class ContentOnlyUpdate
    {
        // AllowEmptyStrings: clearing a block's content is a legitimate save; only a JSON null
        // (which would bypass the non-nullable default) gets the automatic 400.
        [Required(AllowEmptyStrings = true)]
        public string Content { get; set; } = string.Empty;
        public DateTime? LastModifiedOn { get; set; }

        /// <summary>
        /// When non-null, the block's attached files are replaced with this set (delta add/remove).
        /// Null leaves attachments untouched, so a content-only save need not resend them.
        /// </summary>
        public List<Guid>? FileGuids { get; set; }
    }

    public class DiffAgainstHistoryRequest
    {
        [Required(AllowEmptyStrings = true)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Client-supplied fields for a content-scoped inline upload. Deliberately minimal: the folder,
    /// public flag, and permissions are taken from the block server-side, so a delegated editor can
    /// only influence the stored name and whether to overwrite an orphaned same-named disk file.
    /// </summary>
    public class ContentBlockFileUpload
    {
        public string? FileName { get; set; }
        public bool? Overwrite { get; set; }
    }
}
