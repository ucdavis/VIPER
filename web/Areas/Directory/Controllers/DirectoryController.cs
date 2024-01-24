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

namespace Viper.Areas.Directory.Controllers
{
    //TODO: Create the Directory Delegate Users role and add anyone with access to delegate roles
    [Area("Directory")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Delegate Users", Policy = "2faAuthentication")]
    public class DirectoryController : AreaController
    {
        private readonly Classes.SQLContext.RAPSContext _RAPSContext;
        private RAPSSecurityService _securityService;
        public Classes.SQLContext.AAUDContext _aaud;
        public IUserHelper UserHelper;

        public DirectoryController(Classes.SQLContext.RAPSContext context)
        {
            _RAPSContext = context;
            _securityService = new RAPSSecurityService(context);
            _aaud = new AAUDContext();
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("/[area]/")]
        public async Task<ActionResult> Index()
        {
            return await Task.Run(() => View("~/Areas/Directory/Views/Index.cshtml"));
        }

        [Route("/[area]/[action]")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav(int? roleId, int? permissionId, string? memberId, string instance = "VIPER")
        {
            var nav = new List<NavMenuItem>();
            return nav;
        }

        /// <summary>
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        /// <returns></returns>
        [Route("/[area]/search/{search}")]
        public async Task<ActionResult<IEnumerable<IndividualSearchResult>>> Get(string search)
        {
            var individuals = await _aaud.AaudUsers
                     .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                         || (u.MailId != null && u.MailId.Contains(search))
                         || (u.LoginId != null && u.LoginId.Contains(search))
            )
            .Where(u => u.Current != 0)
            .OrderBy(u => u.DisplayLastName)
            .ThenBy(u => u.DisplayFirstName)
                     .ToListAsync();
            List<IndividualSearchResult> results = new();
            individuals.ForEach(m =>
            {
                results.Add(new IndividualSearchResult()
                {
                    ClientId = m.ClientId,
                    MothraId = m.MothraId,
                    LoginId = m.LoginId,
                    MailId = m.MailId,
                    SpridenId = m.SpridenId,
                    Pidm = m.Pidm,
                    EmployeeId = m.EmployeeId,
                    VmacsId = m.VmacsId,
                    VmcasId = m.VmcasId,
                    UnexId = m.UnexId,
                    MivId = m.MivId,
                    LastName = m.LastName,
                    FirstName = m.FirstName,
                    MiddleName = m.MiddleName,
                    DisplayLastName = m.DisplayLastName,
                    DisplayFirstName = m.DisplayFirstName,
                    DisplayMiddleName = m.DisplayMiddleName,
                    DisplayFullName = m.DisplayFullName,
                    Name = m.DisplayFullName,
                    CurrentStudent = m.CurrentStudent,
                    FutureStudent = m.FutureStudent,
                    CurrentEmployee = m.CurrentEmployee,
                    FutureEmployee = m.FutureEmployee,
                    StudentTerm = m.StudentTerm,
                    EmployeeTerm = m.EmployeeTerm,
                    PpsId = m.PpsId,
                    StudentPKey = m.StudentPKey,
                    EmployeePKey = m.EmployeePKey,
                    Current = m.Current,
                    Future = m.Future,
                    IamId = m.IamId,
                    Ross = m.Ross,
                    Added = m.Added
                });
            });
            results.ForEach(r =>
            {
                LdapUser l = new LdapService().GetUser(r.LoginId);
                r.Title = l.Title;
                r.Department = l.Department;
            });

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
