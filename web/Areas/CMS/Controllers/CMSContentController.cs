using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models;
using Viper.Models.VIPER;
using Viper.Services;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    //NOTE: no controller-wide permissions being checked because CMS content can be publically accessible
    [Route("/api/CMS/content")]
    public class CMSContentController : ApiController
    {
        private readonly VIPERContext _context;
        private readonly RAPSContext _rapsContext;
        private readonly IHtmlSanitizerService _sanitizerService;
        private readonly ICmsContentBlockService _blockService;
        private readonly ICmsFileStorageService _storage;
        private readonly IUserHelper _userHelper;

        public CMSContentController(VIPERContext context, RAPSContext rapsContext,
            IHtmlSanitizerService sanitizerService, ICmsContentBlockService blockService,
            ICmsFileStorageService storage, IUserHelper userHelper)
        {
            _context = context;
            _rapsContext = rapsContext;
            _sanitizerService = sanitizerService;
            _blockService = blockService;
            _storage = storage;
            _userHelper = userHelper;
        }

        //GET: content
        [HttpGet]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<List<ContentBlockDto>>> GetContentBlocks(
            string status = "active",
            string? system = null,
            string? viperSectionPath = null,
            string? search = null,
            bool? isPublic = null,
            CancellationToken ct = default)
        {
            return await _blockService.GetContentBlocksAsync(status, system, viperSectionPath, search, isPublic, ct);
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

        //GET: content/5
        [HttpGet("{contentBlockId:int}")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<ContentBlockDto>> GetContentBlock(int contentBlockId, CancellationToken ct = default)
        {
            var block = await _blockService.GetContentBlockAsync(contentBlockId, ct);
            if (block == null)
            {
                return NotFound();
            }
            return block;
        }

        //GET: content/fn/{friendlyName} — public display endpoint; permission filtering happens
        //inside GetContentBlocksAllowed (public flag, block permissions, or CMS admin).
        [HttpGet("fn/{friendlyName}")]
        public ActionResult<ContentBlock?> GetContentBlockByFn(string friendlyName)
        {
            // status: 1 = active only. A public display endpoint must never serve soft-deleted
            // blocks (passing null would include DeletedOn != null rows).
            var blocks = new Data.CMS(_context, _rapsContext, _sanitizerService)
                .GetContentBlocksAllowed(null, friendlyName, null, null, null, null, null, 1)?.ToList();
            if (blocks == null || blocks.Count == 0)
            {
                return NotFound();
            }

            return blocks[0];
        }

        //GET: content/5/history
        [HttpGet("{contentBlockId:int}/history")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<List<ContentHistoryListItemDto>>> GetHistory(int contentBlockId, CancellationToken ct = default)
        {
            return await _blockService.GetHistoryAsync(contentBlockId, ct);
        }

        //GET: content/5/history/12
        [HttpGet("{contentBlockId:int}/history/{contentHistoryId:int}")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<ContentHistoryDto>> GetHistoryVersion(int contentBlockId, int contentHistoryId,
            CancellationToken ct = default)
        {
            var version = await _blockService.GetHistoryVersionAsync(contentBlockId, contentHistoryId, ct);
            if (version == null)
            {
                return NotFound();
            }
            return version;
        }

        //GET: content/5/history/12/diff — rendered diff of this version against the previous version
        [HttpGet("{contentBlockId:int}/history/{contentHistoryId:int}/diff")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<ContentHistoryDiffDto>> GetHistoryVersionDiff(int contentBlockId, int contentHistoryId,
            CancellationToken ct = default)
        {
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
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<ContentHistoryDiffDto>> DiffAgainstHistoryVersion(int contentBlockId, int contentHistoryId,
            DiffAgainstHistoryRequest request, CancellationToken ct = default)
        {
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

        //PATCH: content/5/content — quick save for content-only edits
        [HttpPatch("{contentBlockId:int}/content")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<ContentBlockDto>> UpdateContentOnly(int contentBlockId,
            ContentOnlyUpdate update, CancellationToken ct = default)
        {
            try
            {
                var updated = await _blockService.UpdateContentOnlyAsync(contentBlockId, update.Content, update.LastModifiedOn, ct);
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
                // Permanent delete removes history and cannot be undone; admin only.
                if (!_userHelper.HasPermission(_rapsContext, _userHelper.GetCurrentUser(), CmsPermissions.Admin))
                {
                    return ForbidApi();
                }
                return await _blockService.PermanentlyDeleteAsync(contentBlockId, ct) ? NoContent() : NotFound();
            }
            return await _blockService.SoftDeleteAsync(contentBlockId, ct) ? NoContent() : NotFound();
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
        public string Content { get; set; } = string.Empty;
        public DateTime? LastModifiedOn { get; set; }
    }

    public class DiffAgainstHistoryRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
