using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
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
        private readonly IUserHelper _userHelper;

        public CMSContentController(VIPERContext context, RAPSContext rapsContext,
            IHtmlSanitizerService sanitizerService, ICmsContentBlockService blockService, IUserHelper userHelper)
        {
            _context = context;
            _rapsContext = rapsContext;
            _sanitizerService = sanitizerService;
            _blockService = blockService;
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
            CancellationToken ct = default)
        {
            return await _blockService.GetContentBlocksAsync(status, system, viperSectionPath, search, ct);
        }

        //GET: content/section-paths
        [HttpGet("section-paths")]
        [Permission(Allow = CmsPermissions.ManageContentBlocks)]
        public async Task<ActionResult<List<string>>> GetViperSectionPaths(CancellationToken ct = default)
        {
            return await _blockService.GetViperSectionPathsAsync(ct);
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
            var blocks = new Data.CMS(_context, _rapsContext, _sanitizerService)
                .GetContentBlocksAllowed(null, friendlyName, null, null, null, null, null, null)?.ToList();
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
            return await _blockService.RestoreAsync(contentBlockId, ct) ? Ok() : NotFound();
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
}
