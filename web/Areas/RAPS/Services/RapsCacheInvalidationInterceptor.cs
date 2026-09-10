using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    /// <summary>
    /// Evicts cached roles and permissions for whoever a RAPS write affected. Lives on the context
    /// rather than at each call site so every path reaching the database is covered, including ones
    /// added later.
    /// </summary>
    public class RapsCacheInvalidationInterceptor : SaveChangesInterceptor
    {
        private sealed record Affected(HashSet<string> MemberIds, HashSet<int> RoleIds);

        // Keyed on the context instance so a scoped context's pending set cannot leak into another
        // request's. ConditionalWeakTable drops entries when the context is collected.
        private static readonly ConditionalWeakTable<DbContext, Affected> Pending = new();

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            StashAffected(eventData);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            StashAffected(eventData);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Evict(eventData);
            return base.SavedChanges(eventData, result);
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            Evict(eventData);
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        // Read the ChangeTracker before SaveChanges: afterwards deleted entries are detached and their
        // ids are gone.
        private static void StashAffected(DbContextEventData eventData)
        {
            var context = eventData.Context;
            if (context == null)
            {
                return;
            }

            var affected = new Affected(new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<int>());

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                {
                    continue;
                }

                switch (entry.Entity)
                {
                    // A membership or an individual grant changes exactly one person.
                    case TblRoleMember roleMember:
                        Add(affected.MemberIds, roleMember.MemberId);
                        break;
                    case TblMemberPermission memberPermission:
                        Add(affected.MemberIds, memberPermission.MemberId);
                        break;
                    // A role's permissions change everyone currently in that role.
                    case TblRolePermission rolePermission:
                        affected.RoleIds.Add(rolePermission.RoleId);
                        break;
                    case TblRole role:
                        affected.RoleIds.Add(role.RoleId);
                        break;
                }
            }

            Pending.Remove(context);
            if (affected.MemberIds.Count > 0 || affected.RoleIds.Count > 0)
            {
                Pending.Add(context, affected);
            }
        }

        private static void Evict(DbContextEventData eventData)
        {
            var context = eventData.Context;
            if (context == null)
            {
                return;
            }

            if (!Pending.TryGetValue(context, out var affected))
            {
                return;
            }
            // Dropped from the table, so the sets below are ours to mutate.
            Pending.Remove(context);

            // Expand role-level changes to that role's current members. Runs after the save, so the
            // membership read reflects what was just committed.
            if (affected.RoleIds.Count > 0 && context is RAPSContext rapsContext)
            {
                // EF.Parameter so a bulk change (role template apply, OU sync) translates through
                // OPENJSON rather than inlining every id.
                List<int> changedRoleIds = affected.RoleIds.ToList();
                foreach (string memberId in rapsContext.TblRoleMembers
                             .AsNoTracking()
                             .Where(rm => EF.Parameter(changedRoleIds).Contains(rm.RoleId))
                             .Select(rm => rm.MemberId)
                             .Distinct()
                             .ToList())
                {
                    Add(affected.MemberIds, memberId);
                }
            }

            foreach (string mothraId in affected.MemberIds)
            {
                UserHelper.ClearCachedRolesAndPermissions(mothraId);
            }
        }

        private static void Add(HashSet<string> set, string? memberId)
        {
            if (!string.IsNullOrEmpty(memberId))
            {
                set.Add(memberId);
            }
        }
    }
}
