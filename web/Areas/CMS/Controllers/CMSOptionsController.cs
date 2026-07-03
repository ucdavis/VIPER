using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Constants;
using Viper.Areas.RAPS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    /// <summary>
    /// Option lists for CMS management forms (permission and person selectors). The RAPS
    /// permissions API requires RAPS roles + 2FA, which CMS content managers don't have,
    /// so the CMS exposes its own read-only lists gated by the CMS manage permissions.
    /// </summary>
    [Route("/api/cms/options")]
    [Permission(Allow = CmsPermissions.AllFiles + "," + CmsPermissions.ManageContentBlocks + "," + CmsPermissions.ManageNavigation)]
    public class CMSOptionsController : ApiController
    {
        private readonly RAPSContext _rapsContext;
        private readonly AAUDContext _aaudContext;

        public CMSOptionsController(RAPSContext rapsContext, AAUDContext aaudContext)
        {
            _rapsContext = rapsContext;
            _aaudContext = aaudContext;
        }

        /// <summary>
        /// All VIPER-instance permission names, for tagging files/blocks/nav items with
        /// required permissions (matches the legacy CMS permission multi-select).
        /// </summary>
        [HttpGet("permissions")]
        public async Task<ActionResult<List<string>>> GetPermissions(CancellationToken ct = default)
        {
            return await _rapsContext.TblPermissions
                .AsNoTracking()
                .Where(RAPSSecurityService.FilterPermissionsToInstance("VIPER"))
                .OrderBy(p => p.Permission)
                .Select(p => p.Permission)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Search current people by name, login id, or mail id for person-level file access.
        /// </summary>
        [HttpGet("people")]
        public async Task<ActionResult<List<CmsPersonOption>>> SearchPeople(string search, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 2)
            {
                return new List<CmsPersonOption>();
            }
            search = search.Trim();

            return await _aaudContext.AaudUsers
                .AsNoTracking()
                .Where(u => u.Current != 0 && u.IamId != null)
                .Where(u => (u.DisplayLastName + ", " + u.DisplayFirstName).Contains(search)
                    || (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                    || (u.LoginId != null && u.LoginId.Contains(search))
                    || (u.MailId != null && u.MailId.Contains(search)))
                .OrderBy(u => u.DisplayLastName)
                .ThenBy(u => u.DisplayFirstName)
                .Take(25)
                .Select(u => new CmsPersonOption
                {
                    IamId = u.IamId!,
                    Name = u.DisplayLastName + ", " + u.DisplayFirstName,
                    LoginId = u.LoginId,
                    MailId = u.MailId
                })
                .ToListAsync(ct);
        }
    }

    public class CmsPersonOption
    {
        public string IamId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? LoginId { get; set; }
        public string? MailId { get; set; }
    }
}
