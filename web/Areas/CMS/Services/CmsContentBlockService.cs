using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Models;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;
using Viper.Services;

namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// Thrown when an update is based on a stale copy of a block (someone else saved since
    /// the editor loaded it). Controllers translate this to 409 Conflict.
    /// </summary>
    public class CmsConcurrencyException : InvalidOperationException
    {
        public CmsConcurrencyException(string message) : base(message) { }
    }

    public interface ICmsContentBlockService
    {
        Task<List<ContentBlockDto>> GetContentBlocksAsync(string status, string? system, string? viperSectionPath,
            string? search, bool? isPublic = null, CancellationToken ct = default);

        Task<ContentBlockDto?> GetContentBlockAsync(int contentBlockId, CancellationToken ct = default);

        Task<ContentBlockDto> CreateContentBlockAsync(CMSBlockAddEdit request, CancellationToken ct = default);

        Task<ContentBlockDto?> UpdateContentBlockAsync(int contentBlockId, CMSBlockAddEdit request, CancellationToken ct = default);

        Task<ContentBlockDto?> UpdateContentOnlyAsync(int contentBlockId, string content, DateTime? lastModifiedOn, CancellationToken ct = default);

        Task<bool> SoftDeleteAsync(int contentBlockId, CancellationToken ct = default);

        Task<bool> RestoreAsync(int contentBlockId, CancellationToken ct = default);

        Task<bool> PermanentlyDeleteAsync(int contentBlockId, CancellationToken ct = default);

        Task<List<ContentHistoryListItemDto>> GetHistoryAsync(int contentBlockId, CancellationToken ct = default);

        Task<ContentHistoryDto?> GetHistoryVersionAsync(int contentBlockId, int contentHistoryId, CancellationToken ct = default);

        Task<List<string>> GetViperSectionPathsAsync(CancellationToken ct = default);
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

        public async Task<List<ContentBlockDto>> GetContentBlocksAsync(string status, string? system,
            string? viperSectionPath, string? search, bool? isPublic = null, CancellationToken ct = default)
        {
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
                query = query.Where(b => (b.Title != null && b.Title.Contains(search))
                    || (b.FriendlyName != null && b.FriendlyName.Contains(search))
                    || (b.Page != null && b.Page.Contains(search))
                    || b.Content.Contains(search));
            }

            // List view: project without Content (it can be large) — the editor loads the full
            // block. Two stages because GetFriendlyURL reads HttpContext and can't translate to SQL.
            var blocks = await query
                .OrderBy(b => b.Title)
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
                        .Where(f => f.File != null)
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

            return blocks.Select(b => new ContentBlockDto
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
                Files = b.Files.Select(f => new ContentBlockFileDto
                {
                    FileGuid = f.FileGuid,
                    FriendlyName = f.FriendlyName,
                    Url = Data.CMS.GetFriendlyURL(f.FriendlyName, f.AllowPublicAccess)
                }).ToList()
            }).ToList();
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
            var fileGuids = request.FileGuids.Distinct().ToList();
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
            var fileGuids = request.FileGuids.Distinct().ToList();
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

        private static void AssertNotStale(ContentBlock block, DateTime? lastModifiedOn)
        {
            if (lastModifiedOn == null)
            {
                throw new ArgumentException("LastModifiedOn is required so concurrent edits can be detected.");
            }
            // Compare to the second: serialized timestamps lose sub-second precision round-tripping
            // through the client.
            if (Math.Abs((block.ModifiedOn - lastModifiedOn.Value).TotalSeconds) >= 1)
            {
                throw new CmsConcurrencyException(
                    $"This content block was modified by {block.ModifiedBy} on {block.ModifiedOn:g}. Reload to get the latest version.");
            }
        }

        private async Task AssertFilesExistAsync(List<Guid> fileGuids, CancellationToken ct)
        {
            if (fileGuids.Count == 0)
            {
                return;
            }
            int existing = await _context.Files.CountAsync(f => fileGuids.Contains(f.FileGuid), ct);
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
            bool taken = await _context.ContentBlocks
                .AnyAsync(b => b.FriendlyName == friendlyName && (exceptBlockId == null || b.ContentBlockId != exceptBlockId), ct);
            if (taken)
            {
                throw new ArgumentException("Friendly name must be unique");
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

        private static List<string> CleanList(ICollection<string> values)
        {
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
