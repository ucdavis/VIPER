using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using Viper.Areas.RAPS.Services;
using Web.Authorization;
using Microsoft.IdentityModel.Tokens;
using Viper.Classes;
using Polly;
using Viper.Classes.SQLContext;
using Viper.Areas.RAPS.Models;
using Viper.Areas.Directory.Models;
using System;
using Viper;
using System.Runtime.Versioning;

namespace Viper.Areas.Directory.Controllers
{
    [Area("Directory")]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure")]
    public class DirectoryController : AreaController
    {
        public Classes.SQLContext.AAUDContext _aaud;
        private readonly RAPSContext? _rapsContext;
        public IUserHelper UserHelper;

        public DirectoryController(Classes.SQLContext.RAPSContext context)
        {
            _aaud = new AAUDContext();
            this._rapsContext = (RAPSContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(RAPSContext));
            UserHelper = new UserHelper();
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("/[area]/")]
        public async Task<ActionResult> Index()
        {
            return await Task.Run(() => View("~/Areas/Directory/Views/Index.cshtml"));
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("/[area]/nav")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav()
        {
            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Instances", IsHeader = true }
            };
            return nav;
        }


        /// <summary>
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        [Route("/[area]/search/{search}")]
        public async Task<ActionResult<IEnumerable<IndividualSearchResult>>> Get(string search)
        {
            var individuals = await _aaud.AaudUsers
                     .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                         || (u.MailId != null && u.MailId.Contains(search))
                         || (u.LoginId != null && u.LoginId.Contains(search))
                         || (u.SpridenId != null && u.SpridenId.Contains(search))
                         || (u.Pidm != null && u.Pidm.Contains(search))
                         || (u.MothraId != null && u.MothraId.Contains(search))
                         || (u.EmployeeId != null && u.EmployeeId.Contains(search))
                         || (u.IamId != null && u.IamId.Contains(search))
            )
            .Where(u => u.Current != 0)
            .OrderBy(u => u.DisplayLastName)
            .ThenBy(u => u.DisplayFirstName)
                     .ToListAsync();
            List<IndividualSearchResult> results = new();
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            individuals.ForEach(m =>
            {
                dynamic indiv = new IndividualSearchResult();
                if (UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail"))
                {
                    indiv = new IndividualSearchResultWithIDs();
                    indiv.SpridenId = m.SpridenId;
                    indiv.Pidm = m.Pidm;
                    indiv.EmployeeId = m.EmployeeId;
                    indiv.VmacsId = m.VmacsId;
                    indiv.UnexId = m.UnexId;
                    indiv.MivId = m.MivId;
                }
                indiv.MothraId = m.MothraId;
                indiv.LoginId = m.LoginId;
                indiv.MailId = m.MailId;
                indiv.LastName = m.LastName;
                indiv.FirstName = m.FirstName;
                indiv.MiddleName = m.MiddleName;
                indiv.DisplayLastName = m.DisplayLastName;
                indiv.DisplayFirstName = m.DisplayFirstName;
                indiv.DisplayMiddleName = m.DisplayMiddleName;
                indiv.DisplayFullName = m.DisplayFullName;
                indiv.Name = m.DisplayFullName;
                indiv.CurrentStudent = m.CurrentStudent;
                indiv.FutureStudent = m.FutureStudent;
                indiv.CurrentEmployee = m.CurrentEmployee;
                indiv.FutureEmployee = m.FutureEmployee;
                indiv.StudentTerm = m.StudentTerm;
                indiv.EmployeeTerm = m.EmployeeTerm;
                indiv.PpsId = m.PpsId;
                indiv.StudentPKey = m.StudentPKey;
                indiv.EmployeePKey = m.EmployeePKey;
                indiv.Current = m.Current;
                indiv.Future = m.Future;
                indiv.IamId = m.IamId;
                indiv.Ross = m.Ross;
                indiv.Added = m.Added;
                LdapUserContact? l = new LdapService().GetUserContact(m.LoginId);
                if (l != null)
                {
                    indiv.Title = l.title;
                    indiv.Department = l.department;
                    indiv.Phone = l.phone;
                    indiv.Mobile = l.mobile;
                    indiv.UserName = l.username;
                }
                results.Add(indiv);
            });
            return results;
        }

        /// <summary>
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        [Route("/[area]/search/{search}/ucd")]
        public async Task<ActionResult<IEnumerable<IndividualSearchResult>>> GetUCD(string search)
        {
            List<IndividualSearchResult> results = new();
            List<LdapUserContact> ldap = new LdapService().GetUsersContact(search);
            var individuals = await _aaud.AaudUsers
                    .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                        || (u.MailId != null && u.MailId.Contains(search))
                        || (u.LoginId != null && u.LoginId.Contains(search))
                        || (u.SpridenId != null && u.SpridenId.Contains(search))
                        || (u.Pidm != null && u.Pidm.Contains(search))
                        || (u.MothraId != null && u.MothraId.Contains(search))
                        || (u.EmployeeId != null && u.EmployeeId.Contains(search))
                        || (u.IamId != null && u.IamId.Contains(search))
           )
           .Where(u => u.Current != 0)
           .OrderBy(u => u.DisplayLastName)
           .ThenBy(u => u.DisplayFirstName)
                    .ToListAsync();
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            foreach (var l in ldap)
            {
                dynamic indiv = new IndividualSearchResult();
                if (UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail"))
                {
                    indiv = new IndividualSearchResultWithIDs();
                }
                indiv.Title = l.title;
                indiv.Department = l.department;
                indiv.Phone = l.phone;
                indiv.Mobile = l.mobile;
                indiv.UserName = l.username;
                indiv.DisplayFullName = l.displayname;
                indiv.Name = l.displayname;
                individuals.ForEach(m =>
                {
                    if (m.MothraId == l.ucdpersonuuid)
                    {
                        if (UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail"))
                        {
                            indiv = new IndividualSearchResultWithIDs();
                            indiv.SpridenId = m.SpridenId;
                            indiv.Pidm = m.Pidm;
                            indiv.EmployeeId = m.EmployeeId;
                            indiv.VmacsId = m.VmacsId;
                            indiv.UnexId = m.UnexId;
                            indiv.MivId = m.MivId;
                        }
                        indiv.MothraId = m.MothraId;
                        indiv.LoginId = m.LoginId;
                        indiv.MailId = m.MailId;
                        indiv.LastName = m.LastName;
                        indiv.FirstName = m.FirstName;
                        indiv.MiddleName = m.MiddleName;
                        indiv.DisplayLastName = m.DisplayLastName;
                        indiv.DisplayFirstName = m.DisplayFirstName;
                        indiv.DisplayMiddleName = m.DisplayMiddleName;
                        indiv.DisplayFullName = m.DisplayFullName;
                        indiv.Name = m.DisplayFullName;
                        indiv.CurrentStudent = m.CurrentStudent;
                        indiv.FutureStudent = m.FutureStudent;
                        indiv.CurrentEmployee = m.CurrentEmployee;
                        indiv.FutureEmployee = m.FutureEmployee;
                        indiv.StudentTerm = m.StudentTerm;
                        indiv.EmployeeTerm = m.EmployeeTerm;
                        indiv.PpsId = m.PpsId;
                        indiv.StudentPKey = m.StudentPKey;
                        indiv.EmployeePKey = m.EmployeePKey;
                        indiv.Current = m.Current;
                        indiv.Future = m.Future;
                        indiv.IamId = m.IamId;
                        indiv.Ross = m.Ross;
                        indiv.Added = m.Added;
                    }
                });
                results.Add(indiv);
            };
            return results;
        }

        /// <summary>
        /// Directory results
        /// </summary>
        /// <param name="uid">User ID</param>
        /// <returns></returns>
        [Route("/[area]/userInfo/{mothraID}")]
        public async Task<IActionResult> DirectoryResult(string mothraID)
        {
            // pull in the user based on uid
            return await Task.Run(() => View("~/Areas/Directory/Views/UserInfo.cshtml"));
        }
    }
}
