using Microsoft.EntityFrameworkCore;
using HtmlDiffer = HtmlDiff.HtmlDiff;
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

        Task<ContentBlockDto?> UpdateContentOnlyAsync(int contentBlockId, string content, DateTime? lastModifiedOn, CancellationToken ct = default);

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
        private readonly IHtmlSanitizerService _sanitizer;
        private readonly IUserHelper _userHelper;

        public CmsContentBlockService(VIPERContext context, IHtmlSanitizerService sanitizer, IUserHelper userHelper)
        {
            _context = context;
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
                            f.File.FriendlyName,
                            f.File.AllowPublicAccess
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
            ApplyFileDeltas(block, fileGuids);

            await _context.SaveChangesAsync(ct);
            return await GetContentBlockAsync(contentBlockId, ct);
        }

        public async Task<ContentBlockDto?> UpdateContentOnlyAsync(int contentBlockId, string content,
            DateTime? lastModifiedOn, CancellationToken ct = default)
        {
            var block = await LoadBlockAsync(contentBlockId, tracking: true, ct);
            if (block == null)
            {
                return null;
            }

            AssertNotStale(block, lastModifiedOn);
            SavePreviousVersionToHistory(block);
            block.Content = content;
            block.ModifiedOn = DateTime.Now;
            block.ModifiedBy = CurrentLoginId();

            await _context.SaveChangesAsync(ct);
            return await GetContentBlockAsync(contentBlockId, ct);
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

        private void ApplyPermissionDeltas(ContentBlock block, List<string> requested)
        {
            var existing = block.ContentBlockToPermissions.ToList();
            foreach (var cbp in existing.Where(cbp => !requested.Contains(cbp.Permission, StringComparer.OrdinalIgnoreCase)))
            {
                block.ContentBlockToPermissions.Remove(cbp);
                _context.Remove(cbp);
            }
            var existingNames = existing.Select(p => p.Permission).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var permission in requested.Where(p => !existingNames.Contains(p)))
            {
                block.ContentBlockToPermissions.Add(new ContentBlockToPermission
                {
                    ContentBlockId = block.ContentBlockId,
                    Permission = permission
                });
            }
        }

        private void ApplyFileDeltas(ContentBlock block, List<Guid> requested)
        {
            var existing = block.ContentBlockToFiles.ToList();
            foreach (var cbf in existing.Where(cbf => !requested.Contains(cbf.FileGuid)))
            {
                block.ContentBlockToFiles.Remove(cbf);
                _context.Remove(cbf);
            }
            var existingGuids = existing.Select(f => f.FileGuid).ToHashSet();
            foreach (var fileGuid in requested.Where(g => !existingGuids.Contains(g)))
            {
                block.ContentBlockToFiles.Add(new ContentBlockToFile
                {
                    ContentBlockId = block.ContentBlockId,
                    FileGuid = fileGuid
                });
            }
        }

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
