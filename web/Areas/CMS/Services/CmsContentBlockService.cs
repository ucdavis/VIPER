using Microsoft.EntityFrameworkCore;
using HtmlDiffer = HtmlDiff.HtmlDiff;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.VIPER;
using Viper.Services;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsContentBlockService
    {
        Task<(List<ContentBlockDto> Blocks, int Total)> GetContentBlocksAsync(string status, string? system,
            string? viperSectionPath, string? search, bool? isPublic, int page, int perPage, string? sortBy,
            bool descending, CancellationToken ct = default);

        Task<ContentBlockDto?> GetContentBlockAsync(int contentBlockId, CancellationToken ct = default);

        Task<ContentBlockDto> CreateContentBlockAsync(CMSBlockAddEdit request, CancellationToken ct = default);

        Task<ContentBlockDto?> UpdateContentBlockAsync(int contentBlockId, CMSBlockAddEdit request, CancellationToken ct = default);

        Task<ContentBlockDto?> UpdateContentOnlyAsync(int contentBlockId, string content, DateTime? lastModifiedOn,
            List<Guid>? fileGuids = null, CancellationToken ct = default);

        /// <summary>
        /// Whether the current user may edit this block's content and attached files. True for
        /// ManageContentBlocks holders (any block, unchanged), otherwise true only for a live block
        /// whose edit-permission list intersects the user's permissions. Anonymous or missing block
        /// fails closed.
        /// </summary>
        Task<bool> CanEditAsync(int contentBlockId, CancellationToken ct = default);

        /// <summary>
        /// Active blocks the current user may edit by delegation (their permissions intersect the
        /// block's edit-permission list). Projected without Content. Empty for anonymous users.
        /// </summary>
        Task<List<ContentBlockDto>> GetEditableBlocksAsync(CancellationToken ct = default);

        /// <summary>
        /// Active files whose friendly name contains the search term AND the current user could
        /// download (public, unrestricted, permission match, or person grant - mirrors
        /// Data.CMS.CheckFilePermission), for the editor's attach-by-search picker. Minimal DTO
        /// (guid + friendly name only). Empty when the term is shorter than 2 chars or anonymous.
        /// </summary>
        Task<List<CmsAttachableFileDto>> SearchAttachableFilesAsync(string? search, CancellationToken ct = default);

        /// <summary>
        /// The block's section path (its upload folder) without loading the full block - no
        /// content sanitization, files, or permissions. Found=false when the block does not exist.
        /// For callers like the pre-upload name check that run repeatedly while staging files.
        /// </summary>
        Task<(bool Found, string? SectionPath)> GetSectionPathAsync(int contentBlockId, CancellationToken ct = default);

        /// <summary>
        /// The block's upload destination settings (section path, public flag, view permissions)
        /// without loading the full block - no content sanitization or attached files. For the
        /// inline-upload endpoint, which may be called repeatedly during a multi-file upload.
        /// Found=false when the block does not exist.
        /// </summary>
        Task<(bool Found, string? SectionPath, bool AllowPublicAccess, List<string> Permissions)> GetUploadSettingsAsync(
            int contentBlockId, CancellationToken ct = default);

        /// <summary>
        /// Whether the editor may roll back (soft-delete) a file it just uploaded to this block:
        /// null when the file does not exist, false when it is not an eligible rollback (uploaded by
        /// someone else, in a folder other than the block's, or still attached to another block), and
        /// true when the current user uploaded it into this block's folder and no other block uses it.
        /// </summary>
        Task<bool?> IsFileRollbackDeletableAsync(int contentBlockId, Guid fileGuid, CancellationToken ct = default);

        Task<bool> SoftDeleteAsync(int contentBlockId, CancellationToken ct = default);

        Task<bool> RestoreAsync(int contentBlockId, CancellationToken ct = default);

        Task<bool> PermanentlyDeleteAsync(int contentBlockId, CancellationToken ct = default);

        Task<List<ContentHistoryListItemDto>> GetHistoryAsync(int contentBlockId, CancellationToken ct = default);

        Task<ContentHistoryDto?> GetHistoryVersionAsync(int contentBlockId, int contentHistoryId, CancellationToken ct = default);

        Task<ContentHistoryDiffDto?> GetHistoryVersionDiffAsync(int contentBlockId, int contentHistoryId, CancellationToken ct = default);

        Task<ContentHistoryDiffDto?> DiffContentAgainstHistoryAsync(int contentBlockId, int contentHistoryId, string currentContent, CancellationToken ct = default);

        Task<List<ContentHistoryAuditDto>> GetHistoryEntriesAsync(CmsContentHistoryFilter filter, int page, int perPage, CancellationToken ct = default);

        Task<int> GetHistoryEntryCountAsync(CmsContentHistoryFilter filter, CancellationToken ct = default);

        Task<List<string>> GetViperSectionPathsAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Filters for the cross-block edit-history viewer. Mirrors CmsFileAuditFilter so the
    /// content-history page can reuse the file-audit page's filter UX.
    /// </summary>
    public class CmsContentHistoryFilter
    {
        public int? ContentBlockId { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }
    }

    /// <summary>
    /// Management operations for CMS content blocks. Mirrors legacy ColdFusion
    /// ContentBlocks.cfc semantics: content is stored raw and sanitized on render,
    /// and ContentHistory receives the PREVIOUS content/author before each update
    /// so history entries are the older versions, not copies of the current one.
    /// </summary>
    public class CmsContentBlockService : ICmsContentBlockService
    {
        private readonly VIPERContext _context;
        private readonly RAPSContext _rapsContext;
        private readonly IHtmlSanitizerService _sanitizer;
        private readonly IUserHelper _userHelper;

        public CmsContentBlockService(VIPERContext context, RAPSContext rapsContext,
            IHtmlSanitizerService sanitizer, IUserHelper userHelper)
        {
            _context = context;
            _rapsContext = rapsContext;
            _sanitizer = sanitizer;
            _userHelper = userHelper;
        }

        public async Task<(List<ContentBlockDto> Blocks, int Total)> GetContentBlocksAsync(string status, string? system,
            string? viperSectionPath, string? search, bool? isPublic, int page, int perPage, string? sortBy,
            bool descending, CancellationToken ct = default)
        {
            // ApiPagination admits page=0, and Skip with a negative offset throws; clamp both knobs.
            // The upper bound stops a caller from defeating pagination with a giant page size.
            page = Math.Max(page, 1);
            perPage = Math.Clamp(perPage, 1, 500);

            var query = _context.ContentBlocks
                .AsNoTracking()
                .AsSplitQuery()
                .TagWith("CmsContentBlockService.GetContentBlocks")
                .AsQueryable();

            query = status.ToLowerInvariant() switch
            {
                "active" => query.Where(b => b.DeletedOn == null),
                "deleted" => query.Where(b => b.DeletedOn != null),
                _ => query
            };

            if (!string.IsNullOrEmpty(system))
            {
                query = query.Where(b => b.System == system);
            }
            if (!string.IsNullOrEmpty(viperSectionPath))
            {
                query = query.Where(b => b.ViperSectionPath == viperSectionPath);
            }
            if (isPublic != null)
            {
                query = query.Where(b => b.AllowPublicAccess == isPublic);
            }
            if (!string.IsNullOrEmpty(search))
            {
                // Coalescing null columns to "" (never a match: search is non-empty here) keeps
                // this a flat OR chain the SQL and in-memory providers both translate.
                query = query.Where(b => (b.Title ?? "").Contains(search)
                    || (b.FriendlyName ?? "").Contains(search)
                    || (b.Page ?? "").Contains(search)
                    || b.Content.Contains(search));
            }

            int total = await query.CountAsync(ct);

            query = (sortBy?.ToLowerInvariant(), descending) switch
            {
                ("vipersectionpath", false) => query.OrderBy(b => b.ViperSectionPath).ThenBy(b => b.Title),
                ("vipersectionpath", true) => query.OrderByDescending(b => b.ViperSectionPath).ThenBy(b => b.Title),
                ("page", false) => query.OrderBy(b => b.Page).ThenBy(b => b.Title),
                ("page", true) => query.OrderByDescending(b => b.Page).ThenBy(b => b.Title),
                ("modifiedon", false) => query.OrderBy(b => b.ModifiedOn),
                ("modifiedon", true) => query.OrderByDescending(b => b.ModifiedOn),
                (_, true) => query.OrderByDescending(b => b.Title),
                _ => query.OrderBy(b => b.Title)
            };

            // List view: project without Content (it can be large) — the editor loads the full
            // block. Two stages because GetFriendlyURL reads HttpContext and can't translate to SQL.
            var blocks = await query
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(b => new
                {
                    b.ContentBlockId,
                    b.Title,
                    b.System,
                    b.Application,
                    b.Page,
                    b.ViperSectionPath,
                    b.BlockOrder,
                    b.FriendlyName,
                    b.AllowPublicAccess,
                    b.ModifiedOn,
                    b.ModifiedBy,
                    b.DeletedOn,
                    Permissions = b.ContentBlockToPermissions
                        .Select(p => p.Permission)
                        .OrderBy(p => p)
                        .ToList(),
                    Files = b.ContentBlockToFiles
                        .Select(f => new
                        {
                            f.FileGuid,
                            f.File.FriendlyName
                        })
                        .OrderBy(f => f.FriendlyName)
                        .ToList()
                })
                .ToListAsync(ct);

            var dtos = blocks.Select(b => new ContentBlockDto
            {
                ContentBlockId = b.ContentBlockId,
                Title = b.Title,
                System = b.System,
                Application = b.Application,
                Page = b.Page,
                ViperSectionPath = b.ViperSectionPath,
                BlockOrder = b.BlockOrder,
                FriendlyName = b.FriendlyName,
                AllowPublicAccess = b.AllowPublicAccess,
                ModifiedOn = b.ModifiedOn,
                ModifiedBy = b.ModifiedBy,
                DeletedOn = b.DeletedOn,
                Permissions = b.Permissions,
                Files = b.Files.Select(f => CmsContentBlockMapper.ToFileDto(f.FileGuid, f.FriendlyName)).ToList()
            }).ToList();

            return (dtos, total);
        }

        public async Task<ContentBlockDto?> GetContentBlockAsync(int contentBlockId, CancellationToken ct = default)
        {
            var block = await LoadBlockAsync(contentBlockId, tracking: false, ct);
            if (block == null)
            {
                return null;
            }
            var dto = CmsContentBlockMapper.ToDto(block);
            // Sanitize for the editor with the same policy used on render, so what the
            // editor shows matches what viewers will see.
            dto.Content = _sanitizer.Sanitize(dto.Content);
            return dto;
        }

        public async Task<ContentBlockDto> CreateContentBlockAsync(CMSBlockAddEdit request, CancellationToken ct = default)
        {
            await AssertFriendlyNameUniqueAsync(request.FriendlyName, null, ct);
            var fileGuids = (request.FileGuids ?? new List<Guid>()).Distinct().ToList();
            await AssertFilesExistAsync(fileGuids, ct);

            var block = new ContentBlock();
            ApplyScalarFields(block, request);
            block.ModifiedOn = DateTime.Now;
            block.ModifiedBy = CurrentLoginId();

            foreach (var permission in CleanList(request.Permissions))
            {
                block.ContentBlockToPermissions.Add(new ContentBlockToPermission { Permission = permission });
            }
            foreach (var permission in CleanList(request.EditPermissions))
            {
                block.ContentBlockToEditPermissions.Add(new ContentBlockToEditPermission { Permission = permission });
            }
            foreach (var fileGuid in fileGuids)
            {
                block.ContentBlockToFiles.Add(new ContentBlockToFile { FileGuid = fileGuid });
            }

            _context.ContentBlocks.Add(block);
            await _context.SaveChangesAsync(ct);

            return (await GetContentBlockAsync(block.ContentBlockId, ct))!;
        }

        public async Task<ContentBlockDto?> UpdateContentBlockAsync(int contentBlockId, CMSBlockAddEdit request, CancellationToken ct = default)
        {
            var block = await LoadBlockAsync(contentBlockId, tracking: true, ct);
            if (block == null)
            {
                return null;
            }

            AssertNotStale(block, request.LastModifiedOn);
            await AssertFriendlyNameUniqueAsync(request.FriendlyName, contentBlockId, ct);
            var fileGuids = (request.FileGuids ?? new List<Guid>()).Distinct().ToList();
            await AssertFilesExistAsync(fileGuids, ct);

            SavePreviousVersionToHistory(block);
            ApplyScalarFields(block, request);
            block.ModifiedOn = DateTime.Now;
            block.ModifiedBy = CurrentLoginId();

            ApplyPermissionDeltas(block, CleanList(request.Permissions));
            ApplyEditPermissionDeltas(block, CleanList(request.EditPermissions));
            ApplyFileDeltas(block, fileGuids);

            await _context.SaveChangesAsync(ct);
            return await GetContentBlockAsync(contentBlockId, ct);
        }

        public async Task<ContentBlockDto?> UpdateContentOnlyAsync(int contentBlockId, string content,
            DateTime? lastModifiedOn, List<Guid>? fileGuids = null, CancellationToken ct = default)
        {
            var block = await LoadBlockAsync(contentBlockId, tracking: true, ct);
            if (block == null)
            {
                return null;
            }

            AssertNotStale(block, lastModifiedOn);

            // Validate + replace attachments only when the caller sent a set; null leaves the block's
            // files untouched, so a plain content save need not resend them.
            List<Guid>? distinctGuids = null;
            if (fileGuids != null)
            {
                distinctGuids = fileGuids.Distinct().ToList();
                await AssertFilesExistAsync(distinctGuids, ct);
                // Newly-added files only: keeping a manager-attached restricted file must not
                // fail a delegate's content save.
                var alreadyAttached = block.ContentBlockToFiles.Select(f => f.FileGuid).ToHashSet();
                await AssertFilesAttachableAsync(distinctGuids.Where(g => !alreadyAttached.Contains(g)).ToList(),
                    block.ViperSectionPath, ct);
            }

            SavePreviousVersionToHistory(block);
            block.Content = content;
            block.ModifiedOn = DateTime.Now;
            block.ModifiedBy = CurrentLoginId();

            if (distinctGuids != null)
            {
                ApplyFileDeltas(block, distinctGuids);
            }

            await _context.SaveChangesAsync(ct);
            return await GetContentBlockAsync(contentBlockId, ct);
        }

        public async Task<bool> CanEditAsync(int contentBlockId, CancellationToken ct = default)
        {
            var currentUser = _userHelper.GetCurrentUser();
            if (currentUser == null)
            {
                return false;
            }

            // Load only what the decision needs: the block's deleted flag and its edit-permission
            // strings. A missing block fails closed even for managers.
            var block = await _context.ContentBlocks
                .AsNoTracking()
                .Where(b => b.ContentBlockId == contentBlockId)
                .Select(b => new
                {
                    b.DeletedOn,
                    EditPermissions = b.ContentBlockToEditPermissions.Select(p => p.Permission).ToList()
                })
                .FirstOrDefaultAsync(ct);
            if (block == null)
            {
                return false;
            }

            // Resolve the user's permissions once into a case-insensitive set, mirroring
            // Data.CMS.CheckFilePermission, then reuse it for both the manager check and the
            // delegated-edit-list intersection instead of re-resolving via HasPermission.
            var userPermissions = _userHelper.GetAllPermissions(_rapsContext, currentUser)
                .Select(p => p.Permission)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Full manage overrides everything (managers edit any block via the list page).
            if (userPermissions.Contains(CmsPermissions.ManageContentBlocks))
            {
                return true;
            }

            // Delegated editing is explicit: an empty edit list is manager-only (NOT the view-list's
            // empty-means-all rule), and a soft-deleted block is never editable by a delegate.
            if (block.DeletedOn != null || block.EditPermissions.Count == 0)
            {
                return false;
            }

            return block.EditPermissions.Any(p => userPermissions.Contains(p));
        }

        public async Task<List<ContentBlockDto>> GetEditableBlocksAsync(CancellationToken ct = default)
        {
            var currentUser = _userHelper.GetCurrentUser();
            if (currentUser == null)
            {
                return new List<ContentBlockDto>();
            }

            // Single-resolve: the user's permissions are read once and reused for the manager
            // check below and for every block, instead of HasPermission re-resolving them.
            var userPermissions = _userHelper.GetAllPermissions(_rapsContext, currentUser)
                .Select(p => p.Permission)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Delegated matches only, as documented: managers work from the full list page, and
            // the hub's "Blocks you can edit" card is manager-hidden and expects empty here.
            if (userPermissions.Contains(CmsPermissions.ManageContentBlocks) || userPermissions.Count == 0)
            {
                return new List<ContentBlockDto>();
            }

            // Narrow to active blocks that delegate editing at all, projecting without Content, then
            // intersect in memory so the match is OrdinalIgnoreCase across SQL and test providers.
            var candidates = await _context.ContentBlocks
                .AsNoTracking()
                .Where(b => b.DeletedOn == null && b.ContentBlockToEditPermissions.Any())
                .OrderBy(b => b.Title)
                .Select(b => new
                {
                    b.ContentBlockId,
                    b.Title,
                    b.FriendlyName,
                    b.ViperSectionPath,
                    b.Page,
                    b.ModifiedOn,
                    b.ModifiedBy,
                    EditPermissions = b.ContentBlockToEditPermissions.Select(p => p.Permission).ToList()
                })
                .ToListAsync(ct);

            return candidates
                .Where(b => b.EditPermissions.Any(p => userPermissions.Contains(p)))
                .Select(b => new ContentBlockDto
                {
                    ContentBlockId = b.ContentBlockId,
                    Title = b.Title,
                    FriendlyName = b.FriendlyName,
                    ViperSectionPath = b.ViperSectionPath,
                    Page = b.Page,
                    ModifiedOn = b.ModifiedOn,
                    ModifiedBy = b.ModifiedBy
                })
                .ToList();
        }

        public async Task<List<CmsAttachableFileDto>> SearchAttachableFilesAsync(string? search, CancellationToken ct = default)
        {
            // Require a meaningful term so the picker doesn't return the whole store on an empty query.
            if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 2)
            {
                return new List<CmsAttachableFileDto>();
            }
            var currentUser = _userHelper.GetCurrentUser();
            if (currentUser == null)
            {
                return new List<CmsAttachableFileDto>();
            }
            var term = search.Trim();

            // Name-matched candidates with just the fields the access rules need; the rules are
            // applied in memory (mirroring GetEditableBlocksAsync) so the permission match stays
            // OrdinalIgnoreCase across SQL Server and test providers. The candidate pool is capped
            // so a broad two-character term cannot pull the whole store into memory; accessible
            // files sorting after the cap are missed, which a longer search term resolves.
            var candidates = await _context.Files
                .AsNoTracking()
                .Where(f => f.DeletedOn == null && f.FriendlyName.Contains(term))
                .OrderBy(f => f.FriendlyName)
                .Take(200)
                .Select(f => new
                {
                    f.FileGuid,
                    f.FriendlyName,
                    f.AllowPublicAccess,
                    Permissions = f.FileToPermissions.Select(p => p.Permission).ToList(),
                    People = f.FileToPeople.Select(p => p.IamId).ToList()
                })
                .ToListAsync(ct);

            var userPermissions = _userHelper.GetAllPermissions(_rapsContext, currentUser)
                .Select(p => p.Permission)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            // "any SVMSecure user" reuses the already-resolved set rather than re-running the
            // (4-query) permission resolve HasPermission would trigger on every keystroke here.
            bool hasSvmSecure = userPermissions.Contains("SVMSecure");

            // This endpoint is reachable by delegated editors, so it must not leak names/guids of
            // files the user could not download. Same rules as Data.CMS.CheckFilePermission:
            // public, unrestricted (any SVMSecure user), permission match, or explicit person grant.
            return candidates
                .Where(f => f.AllowPublicAccess
                    || (f.Permissions.Count == 0 && hasSvmSecure)
                    || f.Permissions.Any(p => userPermissions.Contains(p))
                    || (currentUser.IamId != null && f.People.Contains(currentUser.IamId)))
                .Take(25)
                .Select(f => new CmsAttachableFileDto
                {
                    FileGuid = f.FileGuid,
                    FriendlyName = f.FriendlyName
                })
                .ToList();
        }

        public async Task<(bool Found, string? SectionPath)> GetSectionPathAsync(int contentBlockId, CancellationToken ct = default)
        {
            var row = await _context.ContentBlocks
                .AsNoTracking()
                .Where(b => b.ContentBlockId == contentBlockId)
                .Select(b => new { b.ViperSectionPath })
                .FirstOrDefaultAsync(ct);
            return row == null ? (false, null) : (true, row.ViperSectionPath);
        }

        public async Task<(bool Found, string? SectionPath, bool AllowPublicAccess, List<string> Permissions)> GetUploadSettingsAsync(
            int contentBlockId, CancellationToken ct = default)
        {
            var row = await _context.ContentBlocks
                .AsNoTracking()
                .Where(b => b.ContentBlockId == contentBlockId)
                .Select(b => new
                {
                    b.ViperSectionPath,
                    b.AllowPublicAccess,
                    Permissions = b.ContentBlockToPermissions.Select(p => p.Permission).ToList()
                })
                .FirstOrDefaultAsync(ct);
            return row == null
                ? (false, null, false, new List<string>())
                : (true, row.ViperSectionPath, row.AllowPublicAccess, row.Permissions);
        }

        public async Task<bool?> IsFileRollbackDeletableAsync(int contentBlockId, Guid fileGuid, CancellationToken ct = default)
        {
            var file = await _context.Files
                .AsNoTracking()
                .Where(f => f.FileGuid == fileGuid)
                .Select(f => new { f.ModifiedBy, f.Folder, f.DeletedOn })
                .FirstOrDefaultAsync(ct);
            if (file == null)
            {
                return null;
            }

            var sectionPath = await _context.ContentBlocks
                .AsNoTracking()
                .Where(b => b.ContentBlockId == contentBlockId)
                .Select(b => b.ViperSectionPath)
                .FirstOrDefaultAsync(ct);

            // Rollback is only for a file the current user just uploaded into this block's folder,
            // and only while it's still live - an already soft-deleted file has nothing to roll back.
            var login = _userHelper.GetCurrentUser()?.LoginId;
            if (file.DeletedOn != null
                || login == null
                || !string.Equals(file.ModifiedBy, login, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(file.Folder, sectionPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Never trash a file another block still attaches — that would break the other block.
            bool attachedElsewhere = await _context.ContentBlockToFiles
                .AnyAsync(cbf => cbf.FileGuid == fileGuid && cbf.ContentBlockId != contentBlockId, ct);
            return !attachedElsewhere;
        }

        public async Task<bool> SoftDeleteAsync(int contentBlockId, CancellationToken ct = default)
        {
            var block = await _context.ContentBlocks.FindAsync(new object[] { contentBlockId }, ct);
            if (block == null)
            {
                return false;
            }
            block.DeletedOn = DateTime.Now;
            block.ModifiedOn = DateTime.Now;
            block.ModifiedBy = CurrentLoginId();
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RestoreAsync(int contentBlockId, CancellationToken ct = default)
        {
            var block = await _context.ContentBlocks.FindAsync(new object[] { contentBlockId }, ct);
            if (block == null)
            {
                return false;
            }
            block.DeletedOn = null;
            block.ModifiedOn = DateTime.Now;
            block.ModifiedBy = CurrentLoginId();
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> PermanentlyDeleteAsync(int contentBlockId, CancellationToken ct = default)
        {
            var block = await _context.ContentBlocks
                .Include(b => b.ContentBlockToPermissions)
                .Include(b => b.ContentBlockToEditPermissions)
                .Include(b => b.ContentBlockToFiles)
                .Include(b => b.ContentHistories)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.ContentBlockId == contentBlockId, ct);
            if (block == null)
            {
                return false;
            }

            _context.RemoveRange(block.ContentHistories);
            _context.RemoveRange(block.ContentBlockToFiles);
            _context.RemoveRange(block.ContentBlockToPermissions);
            _context.RemoveRange(block.ContentBlockToEditPermissions);
            _context.Remove(block);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<ContentHistoryListItemDto>> GetHistoryAsync(int contentBlockId, CancellationToken ct = default)
        {
            return await _context.ContentHistories
                .AsNoTracking()
                .Where(h => h.ContentBlockId == contentBlockId)
                .OrderByDescending(h => h.ModifiedOn)
                .ThenByDescending(h => h.ContentHistoryId)
                .Select(h => new ContentHistoryListItemDto
                {
                    ContentHistoryId = h.ContentHistoryId,
                    ModifiedOn = h.ModifiedOn,
                    ModifiedBy = h.ModifiedBy
                })
                .ToListAsync(ct);
        }

        public async Task<ContentHistoryDto?> GetHistoryVersionAsync(int contentBlockId, int contentHistoryId, CancellationToken ct = default)
        {
            var history = await _context.ContentHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ContentBlockId == contentBlockId && h.ContentHistoryId == contentHistoryId, ct);
            if (history == null)
            {
                return null;
            }
            return new ContentHistoryDto
            {
                ContentHistoryId = history.ContentHistoryId,
                ModifiedOn = history.ModifiedOn,
                ModifiedBy = history.ModifiedBy,
                Content = _sanitizer.Sanitize(history.ContentBlockContent)
            };
        }

        public async Task<ContentHistoryDiffDto?> GetHistoryVersionDiffAsync(int contentBlockId, int contentHistoryId, CancellationToken ct = default)
        {
            var selected = await _context.ContentHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ContentBlockId == contentBlockId && h.ContentHistoryId == contentHistoryId, ct);
            if (selected == null)
            {
                return null;
            }

            // The "previous version" is the next-older history row for this block, by the same
            // newest-first ordering the history list uses. History holds superseded versions only,
            // so the current live block is never the comparison target here.
            var previous = await _context.ContentHistories
                .AsNoTracking()
                .Where(h => h.ContentBlockId == contentBlockId
                    && (h.ModifiedOn < selected.ModifiedOn
                        || (h.ModifiedOn == selected.ModifiedOn && h.ContentHistoryId < selected.ContentHistoryId)))
                .OrderByDescending(h => h.ModifiedOn)
                .ThenByDescending(h => h.ContentHistoryId)
                .FirstOrDefaultAsync(ct);

            if (previous == null)
            {
                // Original version: nothing older to diff against. Return its own sanitized markup.
                return new ContentHistoryDiffDto
                {
                    Content = _sanitizer.Sanitize(selected.ContentBlockContent),
                    HasComparison = false,
                    NewModifiedOn = selected.ModifiedOn,
                    NewModifiedBy = selected.ModifiedBy
                };
            }

            var diffHtml = BuildDiffHtml(previous.ContentBlockContent, selected.ContentBlockContent);
            return new ContentHistoryDiffDto
            {
                Content = diffHtml,
                HasComparison = true,
                HasChanges = DiffHasChanges(diffHtml),
                OldModifiedOn = previous.ModifiedOn,
                OldModifiedBy = previous.ModifiedBy,
                NewModifiedOn = selected.ModifiedOn,
                NewModifiedBy = selected.ModifiedBy
            };
        }

        public async Task<ContentHistoryDiffDto?> DiffContentAgainstHistoryAsync(int contentBlockId, int contentHistoryId, string currentContent, CancellationToken ct = default)
        {
            var selected = await _context.ContentHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ContentBlockId == contentBlockId && h.ContentHistoryId == contentHistoryId, ct);
            if (selected == null)
            {
                return null;
            }

            // The history version is the "old" side and the editor's current draft is the "new" side,
            // so ins/del read as what the draft adds/removes relative to that version.
            var diffHtml = BuildDiffHtml(selected.ContentBlockContent, currentContent);
            return new ContentHistoryDiffDto
            {
                Content = diffHtml,
                HasComparison = true,
                HasChanges = DiffHasChanges(diffHtml),
                OldModifiedOn = selected.ModifiedOn,
                OldModifiedBy = selected.ModifiedBy
            };
        }

        // Sanitize both sides with the render-time policy first so the diff compares what actually
        // renders, then diff, then re-sanitize the merged result. The second pass re-parses through
        // AngleSharp (balancing/closing the malformed tags htmldiff.net can emit) and strips anything
        // off-policy, so we no longer trust the diff library's output — SanitizeDiff keeps only the
        // <ins>/<del> change markers on top of our normal allowlist.
        private string BuildDiffHtml(string oldContent, string newContent)
        {
            var oldSafe = _sanitizer.Sanitize(oldContent);
            var newSafe = _sanitizer.Sanitize(newContent);
            var diff = HtmlDiffer.Execute(oldSafe, newSafe);
            return _sanitizer.SanitizeDiff(diff);
        }

        // htmldiff.net only wraps actual changes in <ins>/<del>; identical inputs come back with
        // neither. Their absence means the two versions render the same.
        private static bool DiffHasChanges(string diffHtml)
        {
            return diffHtml.Contains("<ins", StringComparison.OrdinalIgnoreCase)
                || diffHtml.Contains("<del", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<ContentHistoryAuditDto>> GetHistoryEntriesAsync(CmsContentHistoryFilter filter, int page, int perPage, CancellationToken ct = default)
        {
            // ApiPagination admits page=0, and Skip with a negative offset throws; clamp both knobs.
            // The upper bound stops a caller from defeating pagination with a giant page size.
            page = Math.Max(page, 1);
            perPage = Math.Clamp(perPage, 1, 500);

            return await BuildHistoryQuery(filter)
                .OrderByDescending(x => x.History.ModifiedOn)
                .ThenByDescending(x => x.History.ContentHistoryId)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(x => new ContentHistoryAuditDto
                {
                    ContentHistoryId = x.History.ContentHistoryId,
                    ContentBlockId = x.History.ContentBlockId,
                    Title = x.Block.Title,
                    FriendlyName = x.Block.FriendlyName,
                    Page = x.Block.Page,
                    ModifiedOn = x.History.ModifiedOn,
                    ModifiedBy = x.History.ModifiedBy,
                    BlockDeleted = x.Block.DeletedOn != null
                })
                .ToListAsync(ct);
        }

        public async Task<int> GetHistoryEntryCountAsync(CmsContentHistoryFilter filter, CancellationToken ct = default)
        {
            return await BuildHistoryQuery(filter).CountAsync(ct);
        }

        // Join history to its block so the block's title/page are filterable and projectable
        // (a join, not a correlated subquery). Each row is a superseded prior version.
        private IQueryable<HistoryWithBlock> BuildHistoryQuery(CmsContentHistoryFilter filter)
        {
            var query =
                from h in _context.ContentHistories.AsNoTracking()
                join b in _context.ContentBlocks.AsNoTracking() on h.ContentBlockId equals b.ContentBlockId
                select new HistoryWithBlock { History = h, Block = b };

            if (filter.ContentBlockId != null)
            {
                query = query.Where(x => x.History.ContentBlockId == filter.ContentBlockId);
            }
            if (!string.IsNullOrEmpty(filter.ModifiedBy))
            {
                query = query.Where(x => x.History.ModifiedBy == filter.ModifiedBy);
            }
            if (filter.From != null)
            {
                query = query.Where(x => x.History.ModifiedOn >= filter.From);
            }
            if (filter.To != null)
            {
                // Treat the To date as inclusive through end of day.
                var to = DateRangeHelper.ExclusiveUpperBound(filter.To.Value);
                query = query.Where(x => x.History.ModifiedOn < to);
            }
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(x => (x.Block.Title ?? "").Contains(filter.Search)
                    || (x.Block.FriendlyName ?? "").Contains(filter.Search)
                    || (x.Block.Page ?? "").Contains(filter.Search));
            }
            return query;
        }

        public async Task<List<string>> GetViperSectionPathsAsync(CancellationToken ct = default)
        {
            return await _context.ContentBlocks
                .AsNoTracking()
                .Where(b => b.ViperSectionPath != null && b.ViperSectionPath != "")
                .Select(b => b.ViperSectionPath!)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync(ct);
        }

        private async Task<ContentBlock?> LoadBlockAsync(int contentBlockId, bool tracking, CancellationToken ct)
        {
            var query = _context.ContentBlocks
                .Include(b => b.ContentBlockToPermissions)
                .Include(b => b.ContentBlockToEditPermissions)
                .Include(b => b.ContentBlockToFiles)
                    .ThenInclude(f => f.File)
                .AsSplitQuery();
            if (!tracking)
            {
                query = query.AsNoTracking();
            }
            return await query.FirstOrDefaultAsync(b => b.ContentBlockId == contentBlockId, ct);
        }

        private static void AssertNotStale(ContentBlock block, DateTime? lastModifiedOn) =>
            CmsServiceHelpers.AssertNotStale("content block", block.ModifiedOn, block.ModifiedBy, lastModifiedOn);

        private async Task AssertFilesExistAsync(List<Guid> fileGuids, CancellationToken ct)
        {
            if (fileGuids.Count == 0)
            {
                return;
            }
            int existing = await _context.Files.CountAsync(f => EF.Parameter(fileGuids).Contains(f.FileGuid), ct);
            if (existing != fileGuids.Count)
            {
                throw new ArgumentException("One or more attached files do not exist.");
            }
        }

        // The content-only PATCH is reachable by delegated editors, so newly-attached files must
        // pass the same rules as downloads (public, unrestricted, permission match, person grant -
        // mirrors SearchAttachableFilesAsync), the one exception being a file the caller uploaded
        // for this block's folder (below); otherwise a guessed GUID would leak a restricted file's
        // name through the block's attachment list.
        private async Task AssertFilesAttachableAsync(List<Guid> fileGuids, string? blockFolder, CancellationToken ct)
        {
            if (fileGuids.Count == 0)
            {
                return;
            }
            // Active files only, matching the search: attaching a soft-deleted file by GUID would
            // resurrect it into an active block. A deleted file simply comes back missing here.
            var files = await _context.Files
                .AsNoTracking()
                .Where(f => f.DeletedOn == null && EF.Parameter(fileGuids).Contains(f.FileGuid))
                .Select(f => new
                {
                    f.AllowPublicAccess,
                    f.ModifiedBy,
                    f.Folder,
                    Permissions = f.FileToPermissions.Select(p => p.Permission).ToList(),
                    People = f.FileToPeople.Select(p => p.IamId).ToList()
                })
                .ToListAsync(ct);
            if (files.Count != fileGuids.Count)
            {
                throw new ArgumentException("One or more files cannot be attached because they are deleted or missing.");
            }

            var currentUser = _userHelper.GetCurrentUser();
            var userPermissions = currentUser == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : _userHelper.GetAllPermissions(_rapsContext, currentUser)
                    .Select(p => p.Permission)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            // Derive from the set already resolved above instead of a second permission resolve;
            // the set is empty for an anonymous user, so this stays false there.
            bool hasSvmSecure = userPermissions.Contains("SVMSecure");
            var login = currentUser?.LoginId;

            // A file the current user uploaded into THIS block's folder is always attachable by them. An
            // inline upload inherits the block's VIEW permissions, which a delegated editor need not hold
            // (edit and view lists are independent), so the download-access rules above would otherwise
            // reject the file they just created. ModifiedBy is server-set to the uploader and can't be
            // forged; the folder match scopes the exception to a file uploaded FOR this block, so a
            // delegate cannot move a restricted file they uploaded elsewhere onto a block with broader
            // visibility. This mirrors the uploader+folder check in IsFileRollbackDeletableAsync.
            bool allAttachable = files.All(f => f.AllowPublicAccess
                || (f.Permissions.Count == 0 && hasSvmSecure)
                || f.Permissions.Any(p => userPermissions.Contains(p))
                || (currentUser?.IamId != null && f.People.Contains(currentUser.IamId))
                || (login != null
                    && string.Equals(f.ModifiedBy, login, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(f.Folder, blockFolder, StringComparison.OrdinalIgnoreCase)));
            if (!allAttachable)
            {
                throw new ArgumentException("One or more files cannot be attached because you do not have access to them.");
            }
        }

        private async Task AssertFriendlyNameUniqueAsync(string? friendlyName, int? exceptBlockId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(friendlyName))
            {
                return;
            }
            // Case-insensitive like the left-nav sibling: SQL Server's default collation treats
            // "Name" and "name" as equal, so the pre-check must too (and so must test providers).
            var normalized = friendlyName.ToLowerInvariant();
            bool taken = await _context.ContentBlocks
                .AnyAsync(b => b.FriendlyName != null && b.FriendlyName.ToLower() == normalized
                    && (exceptBlockId == null || b.ContentBlockId != exceptBlockId), ct);
            if (taken)
            {
                throw new ArgumentException("Friendly name must be unique.");
            }
        }

        private void SavePreviousVersionToHistory(ContentBlock block)
        {
            // Store the version being replaced, stamped with ITS author/time (legacy semantics).
            _context.ContentHistories.Add(new ContentHistory
            {
                ContentBlockId = block.ContentBlockId,
                ContentBlockContent = block.Content,
                ModifiedOn = block.ModifiedOn,
                ModifiedBy = block.ModifiedBy
            });
        }

        private static void ApplyScalarFields(ContentBlock block, CMSBlockAddEdit request)
        {
            block.Title = request.Title;
            block.Content = request.Content;
            block.FriendlyName = request.FriendlyName;
            block.System = request.System;
            block.Application = request.Application;
            block.Page = request.Page;
            block.ViperSectionPath = request.ViperSectionPath;
            block.AllowPublicAccess = request.AllowPublicAccess;
            block.BlockOrder = request.BlockOrder;
        }

        // Reconciles a block's child collection (permissions / edit-permissions / files) to the
        // requested key set: remove children whose key is no longer requested, add requested keys not
        // already present. One implementation instead of three hand-kept copies of the same delta.
        private void ApplyChildDeltas<TChild, TKey>(ICollection<TChild> children, List<TKey> requested,
            Func<TChild, TKey> keyOf, Func<TKey, TChild> create, IEqualityComparer<TKey> comparer)
            where TChild : class
        {
            var requestedKeys = new HashSet<TKey>(requested, comparer);
            foreach (var child in children.Where(c => !requestedKeys.Contains(keyOf(c))).ToList())
            {
                children.Remove(child);
                _context.Remove(child);
            }
            var existingKeys = new HashSet<TKey>(children.Select(keyOf), comparer);
            foreach (var key in requestedKeys.Where(k => !existingKeys.Contains(k)))
            {
                children.Add(create(key));
            }
        }

        private void ApplyPermissionDeltas(ContentBlock block, List<string> requested) =>
            ApplyChildDeltas(block.ContentBlockToPermissions, requested, p => p.Permission,
                permission => new ContentBlockToPermission { ContentBlockId = block.ContentBlockId, Permission = permission },
                StringComparer.OrdinalIgnoreCase);

        private void ApplyEditPermissionDeltas(ContentBlock block, List<string> requested) =>
            ApplyChildDeltas(block.ContentBlockToEditPermissions, requested, p => p.Permission,
                permission => new ContentBlockToEditPermission { ContentBlockId = block.ContentBlockId, Permission = permission },
                StringComparer.OrdinalIgnoreCase);

        private void ApplyFileDeltas(ContentBlock block, List<Guid> requested) =>
            ApplyChildDeltas(block.ContentBlockToFiles, requested, f => f.FileGuid,
                fileGuid => new ContentBlockToFile { ContentBlockId = block.ContentBlockId, FileGuid = fileGuid },
                EqualityComparer<Guid>.Default);

        private string CurrentLoginId()
        {
            return _userHelper.GetCurrentUser()?.LoginId ?? "unknown";
        }

        // Wrapper so the history/block join can be filtered and ordered before projection.
        private sealed class HistoryWithBlock
        {
            public ContentHistory History { get; set; } = null!;
            public ContentBlock Block { get; set; } = null!;
        }

        private static List<string> CleanList(ICollection<string>? values) => CmsServiceHelpers.CleanList(values);
    }
}
