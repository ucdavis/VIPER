using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsLeftNavService
    {
        Task<List<LeftNavMenuDto>> GetMenusAsync(string? system, string? viperSectionPath, string? search, CancellationToken ct = default);

        Task<LeftNavMenuDto?> GetMenuAsync(int leftNavMenuId, CancellationToken ct = default);

        Task<LeftNavMenuDto> CreateMenuAsync(LeftNavMenuAddEdit request, CancellationToken ct = default);

        Task<LeftNavMenuDto?> UpdateMenuAsync(int leftNavMenuId, LeftNavMenuAddEdit request, CancellationToken ct = default);

        Task<bool> DeleteMenuAsync(int leftNavMenuId, CancellationToken ct = default);

        /// <summary>
        /// Replace the menu's items with the supplied list: items with id 0 are added,
        /// existing ids are updated, omitted ids are deleted, and DisplayOrder follows
        /// the array order. Matches the legacy editor's batch save.
        /// </summary>
        Task<LeftNavMenuDto?> SaveItemsAsync(int leftNavMenuId, List<LeftNavItemEdit> items, CancellationToken ct = default);
    }

    /// <summary>
    /// Management operations for CMS left navigation menus (legacy LeftNavs.cfc /
    /// LeftNavAjax.cfc). Menu deletes cascade to items and item permissions; there is
    /// no soft delete for menus, matching legacy.
    /// </summary>
    public class CmsLeftNavService : ICmsLeftNavService
    {
        private readonly VIPERContext _context;
        private readonly IUserHelper _userHelper;

        public CmsLeftNavService(VIPERContext context, IUserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }

        public async Task<List<LeftNavMenuDto>> GetMenusAsync(string? system, string? viperSectionPath, string? search, CancellationToken ct = default)
        {
            var query = _context.LeftNavMenus
                .AsNoTracking()
                .Include(m => m.LeftNavItems)
                    .ThenInclude(i => i.LeftNavItemToPermissions)
                .AsSplitQuery()
                .TagWith("CmsLeftNavService.GetMenus")
                .AsQueryable();

            if (!string.IsNullOrEmpty(system))
            {
                query = query.Where(m => m.System == system);
            }
            if (!string.IsNullOrEmpty(viperSectionPath))
            {
                query = query.Where(m => m.ViperSectionPath == viperSectionPath);
            }
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => (m.MenuHeaderText != null && m.MenuHeaderText.Contains(search))
                    || (m.FriendlyName != null && m.FriendlyName.Contains(search))
                    || (m.Page != null && m.Page.Contains(search)));
            }

            var menus = await query
                .OrderBy(m => m.MenuHeaderText)
                .ToListAsync(ct);
            return menus.Select(ToDto).ToList();
        }

        public async Task<LeftNavMenuDto?> GetMenuAsync(int leftNavMenuId, CancellationToken ct = default)
        {
            var menu = await LoadMenuAsync(leftNavMenuId, tracking: false, ct);
            return menu == null ? null : ToDto(menu);
        }

        public async Task<LeftNavMenuDto> CreateMenuAsync(LeftNavMenuAddEdit request, CancellationToken ct = default)
        {
            await AssertFriendlyNameUniqueAsync(request.FriendlyName, null, ct);

            var menu = new LeftNavMenu();
            ApplyMenuFields(menu, request);
            menu.ModifiedOn = DateTime.Now;
            menu.ModifiedBy = CurrentLoginId();

            _context.LeftNavMenus.Add(menu);
            await _context.SaveChangesAsync(ct);
            return ToDto(menu);
        }

        public async Task<LeftNavMenuDto?> UpdateMenuAsync(int leftNavMenuId, LeftNavMenuAddEdit request, CancellationToken ct = default)
        {
            var menu = await LoadMenuAsync(leftNavMenuId, tracking: true, ct);
            if (menu == null)
            {
                return null;
            }

            AssertNotStale(menu, request.LastModifiedOn);
            await AssertFriendlyNameUniqueAsync(request.FriendlyName, leftNavMenuId, ct);

            ApplyMenuFields(menu, request);
            menu.ModifiedOn = DateTime.Now;
            menu.ModifiedBy = CurrentLoginId();

            await _context.SaveChangesAsync(ct);
            return ToDto(menu);
        }

        public async Task<bool> DeleteMenuAsync(int leftNavMenuId, CancellationToken ct = default)
        {
            var menu = await LoadMenuAsync(leftNavMenuId, tracking: true, ct);
            if (menu == null)
            {
                return false;
            }

            foreach (var item in menu.LeftNavItems)
            {
                _context.RemoveRange(item.LeftNavItemToPermissions);
            }
            _context.RemoveRange(menu.LeftNavItems);
            _context.Remove(menu);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<LeftNavMenuDto?> SaveItemsAsync(int leftNavMenuId, List<LeftNavItemEdit> items, CancellationToken ct = default)
        {
            // Reject duplicate ids up front so every caller gets the guard (was in the controller).
            if (items.Where(i => i.LeftNavItemId > 0).GroupBy(i => i.LeftNavItemId).Any(g => g.Count() > 1))
            {
                throw new ArgumentException("Duplicate item ids are not allowed.");
            }

            // Links need text; a header may be blank - legacy menus use empty headers as spacer
            // rows and the display side still renders them that way.
            if (items.Any(i => !i.IsHeader && string.IsNullOrWhiteSpace(i.MenuItemText)))
            {
                throw new ArgumentException("Every link needs text - fill in the empty link before saving.");
            }

            var menu = await LoadMenuAsync(leftNavMenuId, tracking: true, ct);
            if (menu == null)
            {
                return null;
            }

            var existingById = menu.LeftNavItems.ToDictionary(i => i.LeftNavItemId);

            // A supplied id that isn't in this menu means the client's copy is stale (the item was
            // deleted, or belongs to another menu). Reject rather than silently resurrecting it as a
            // new row.
            if (items.Any(i => i.LeftNavItemId > 0 && !existingById.ContainsKey(i.LeftNavItemId)))
            {
                throw new InvalidOperationException(
                    "One or more items no longer exist in this menu. Reload and try again.");
            }

            var requestedIds = items.Where(i => i.LeftNavItemId > 0).Select(i => i.LeftNavItemId).ToHashSet();

            // Delete items not present in the request.
            foreach (var item in menu.LeftNavItems.Where(i => !requestedIds.Contains(i.LeftNavItemId)).ToList())
            {
                _context.RemoveRange(item.LeftNavItemToPermissions);
                _context.Remove(item);
                menu.LeftNavItems.Remove(item);
            }

            int order = 1;
            foreach (var requested in items)
            {
                if (requested.LeftNavItemId > 0 && existingById.TryGetValue(requested.LeftNavItemId, out var existing))
                {
                    existing.MenuItemText = requested.MenuItemText;
                    existing.IsHeader = requested.IsHeader;
                    existing.Url = requested.IsHeader ? null : requested.Url;
                    existing.DisplayOrder = order;
                    ApplyItemPermissionDeltas(existing, CleanList(requested.Permissions));
                }
                else
                {
                    var newItem = new LeftNavItem
                    {
                        LeftNavMenuId = leftNavMenuId,
                        MenuItemText = requested.MenuItemText,
                        IsHeader = requested.IsHeader,
                        Url = requested.IsHeader ? null : requested.Url,
                        DisplayOrder = order
                    };
                    foreach (var permission in CleanList(requested.Permissions))
                    {
                        newItem.LeftNavItemToPermissions.Add(new LeftNavItemToPermission { Permission = permission });
                    }
                    menu.LeftNavItems.Add(newItem);
                }
                order++;
            }

            menu.ModifiedOn = DateTime.Now;
            menu.ModifiedBy = CurrentLoginId();

            await _context.SaveChangesAsync(ct);
            return await GetMenuAsync(leftNavMenuId, ct);
        }

        // FriendlyName resolves menus in LayoutController via FirstOrDefault, so duplicates make
        // resolution nondeterministic. Reject them case-insensitively at write time. SQL Server's
        // default collation is case-insensitive; ToLower keeps the check correct on other providers.
        private async Task AssertFriendlyNameUniqueAsync(string? friendlyName, int? exceptMenuId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(friendlyName))
            {
                return;
            }

            // Invariant on the client side (culture-sensitive ToLower mishandles e.g. Turkish i);
            // the EF-side ToLower below translates to SQL LOWER and never runs in .NET.
            var normalized = friendlyName.ToLowerInvariant();
            bool taken = await _context.LeftNavMenus
                .AnyAsync(m => m.FriendlyName != null && m.FriendlyName.ToLower() == normalized
                    && (exceptMenuId == null || m.LeftNavMenuId != exceptMenuId), ct);
            if (taken)
            {
                throw new ArgumentException("Friendly name must be unique.");
            }
        }

        // Mirrors CmsFileService/CmsContentBlockService: a missing stamp is a 400 (the client must
        // send it) and a stale one is a 409 (someone saved since the editor loaded the menu).
        private static void AssertNotStale(LeftNavMenu menu, DateTime? lastModifiedOn)
        {
            if (lastModifiedOn == null)
            {
                throw new ArgumentException("LastModifiedOn is required so concurrent edits can be detected.");
            }
            // Compare to the second: serialized timestamps lose sub-second precision round-tripping
            // through the client.
            if (Math.Abs((menu.ModifiedOn - lastModifiedOn.Value).TotalSeconds) >= 1)
            {
                throw new CmsConcurrencyException(
                    $"This menu was modified by {menu.ModifiedBy} on {menu.ModifiedOn:g}. Reload to get the latest version.");
            }
        }

        private async Task<LeftNavMenu?> LoadMenuAsync(int leftNavMenuId, bool tracking, CancellationToken ct)
        {
            var query = _context.LeftNavMenus
                .Include(m => m.LeftNavItems)
                    .ThenInclude(i => i.LeftNavItemToPermissions)
                .AsSplitQuery();
            if (!tracking)
            {
                query = query.AsNoTracking();
            }
            return await query.FirstOrDefaultAsync(m => m.LeftNavMenuId == leftNavMenuId, ct);
        }

        private void ApplyItemPermissionDeltas(LeftNavItem item, List<string> requested)
        {
            var existing = item.LeftNavItemToPermissions.ToList();
            foreach (var p in existing.Where(p => !requested.Contains(p.Permission, StringComparer.OrdinalIgnoreCase)))
            {
                item.LeftNavItemToPermissions.Remove(p);
                _context.Remove(p);
            }
            var existingNames = existing.Select(p => p.Permission).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var permission in requested.Where(p => !existingNames.Contains(p)))
            {
                item.LeftNavItemToPermissions.Add(new LeftNavItemToPermission
                {
                    LeftNavItemId = item.LeftNavItemId,
                    Permission = permission
                });
            }
        }

        private static void ApplyMenuFields(LeftNavMenu menu, LeftNavMenuAddEdit request)
        {
            menu.MenuHeaderText = request.MenuHeaderText;
            menu.System = request.System;
            menu.ViperSectionPath = request.ViperSectionPath;
            menu.Page = request.Page;
            menu.FriendlyName = request.FriendlyName;
        }

        private static LeftNavMenuDto ToDto(LeftNavMenu menu)
        {
            return new LeftNavMenuDto
            {
                LeftNavMenuId = menu.LeftNavMenuId,
                MenuHeaderText = menu.MenuHeaderText,
                System = menu.System,
                ViperSectionPath = menu.ViperSectionPath,
                Page = menu.Page,
                FriendlyName = menu.FriendlyName,
                ModifiedOn = menu.ModifiedOn,
                ModifiedBy = menu.ModifiedBy,
                Items = menu.LeftNavItems
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new LeftNavItemDto
                    {
                        LeftNavItemId = i.LeftNavItemId,
                        MenuItemText = i.MenuItemText,
                        IsHeader = i.IsHeader,
                        Url = i.Url,
                        DisplayOrder = i.DisplayOrder,
                        Permissions = i.LeftNavItemToPermissions.Select(p => p.Permission).OrderBy(p => p).ToList()
                    })
                    .ToList()
            };
        }

        private string CurrentLoginId()
        {
            return _userHelper.GetCurrentUser()?.LoginId ?? "unknown";
        }

        private static List<string> CleanList(List<string> values)
        {
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
