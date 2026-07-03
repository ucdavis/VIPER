using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Models;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.VIPER;
using File = Viper.Models.VIPER.File;

namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// File audit actions, matching the legacy ColdFusion FileAudit.cfc action names so the
    /// shared fileAudit table stays consistent across both systems.
    /// </summary>
    public static class CmsFileAuditActions
    {
        public const string UploadFile = "UploadFile";
        public const string AddFile = "AddFile";
        public const string EditFile = "EditFile";
        public const string DeleteFile = "DeleteFile";
        public const string CancelDelete = "CancelDelete";
        public const string ImportFile = "ImportFile";
        public const string AccessFile = "AccessFile";
        public const string AccessFileDenied = "AccessFileDenied";
    }

    public class CmsFileAuditFilter
    {
        public Guid? FileGuid { get; set; }
        public string? Action { get; set; }
        public string? LoginId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }
    }

    public interface ICmsFileAuditService
    {
        /// <summary>Add an audit row for a file operation. Does not call SaveChanges.</summary>
        void Audit(File file, string action, string detail = "");

        Task<List<FileAudit>> GetAuditEntriesAsync(CmsFileAuditFilter filter, int page, int perPage, CancellationToken ct = default);

        Task<int> GetAuditEntryCountAsync(CmsFileAuditFilter filter, CancellationToken ct = default);
    }

    public class CmsFileAuditService : ICmsFileAuditService
    {
        private readonly VIPERContext _context;
        private readonly IUserHelper _userHelper;

        public CmsFileAuditService(VIPERContext context, IUserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }

        public void Audit(File file, string action, string detail = "")
        {
            var user = _userHelper.GetCurrentUser();
            var metaData = new CMSFileMetaData
            {
                Folder = file.Folder,
                Encrypted = file.Encrypted,
                Public = file.AllowPublicAccess,
                Modified = file.ModifiedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedBy = file.ModifiedBy
            };

            _context.FileAudits.Add(new FileAudit
            {
                Timestamp = DateTime.Now,
                Loginid = user?.LoginId,
                Action = action,
                Detail = detail,
                FileGuid = file.FileGuid,
                FilePath = file.FilePath,
                IamId = user?.IamId,
                FileMetaData = JsonSerializer.Serialize(metaData),
                ClientData = JsonSerializer.Serialize(_userHelper.GetClientData())
            });
        }

        public async Task<List<FileAudit>> GetAuditEntriesAsync(CmsFileAuditFilter filter, int page, int perPage, CancellationToken ct = default)
        {
            // ApiPagination admits page=0, and Skip with a negative offset throws; clamp both knobs.
            page = Math.Max(page, 1);
            perPage = Math.Max(perPage, 1);

            return await BuildQuery(filter)
                .OrderByDescending(a => a.Timestamp)
                .ThenByDescending(a => a.AuditId)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync(ct);
        }

        public async Task<int> GetAuditEntryCountAsync(CmsFileAuditFilter filter, CancellationToken ct = default)
        {
            return await BuildQuery(filter).CountAsync(ct);
        }

        private IQueryable<FileAudit> BuildQuery(CmsFileAuditFilter filter)
        {
            var query = _context.FileAudits.AsNoTracking();
            if (filter.FileGuid != null)
            {
                query = query.Where(a => a.FileGuid == filter.FileGuid);
            }
            if (!string.IsNullOrEmpty(filter.Action))
            {
                query = query.Where(a => a.Action == filter.Action);
            }
            if (!string.IsNullOrEmpty(filter.LoginId))
            {
                query = query.Where(a => a.Loginid == filter.LoginId);
            }
            if (filter.From != null)
            {
                query = query.Where(a => a.Timestamp >= filter.From);
            }
            if (filter.To != null)
            {
                // Treat the To date as inclusive through end of day.
                var to = DateRangeHelper.ExclusiveUpperBound(filter.To.Value);
                query = query.Where(a => a.Timestamp < to);
            }
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(a => a.FilePath.Contains(filter.Search)
                    || (a.Detail != null && a.Detail.Contains(filter.Search)));
            }
            return query;
        }
    }
}
