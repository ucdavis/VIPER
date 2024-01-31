﻿using Microsoft.AspNetCore.Authorization;
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
            individuals.ForEach(m =>
            {
                results.Add(new IndividualSearchResult()
                {
                    MothraId = m.MothraId,
                    LoginId = m.LoginId,
                    MailId = m.MailId,
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
                    Added = m.Added,
                    SpridenId = m.SpridenId,
                    Pidm = m.Pidm,
                    EmployeeId = m.EmployeeId,
                    VmacsId = m.VmacsId,
                    UnexId = m.UnexId,
                    MivId = m.MivId,
                });
            });
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            if (!UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail"))
            {
                results.ForEach(r =>
                {
                    r.SpridenId = null;
                    r.Pidm = null;
                    r.EmployeeId = null;
                    r.VmacsId = null;
                    r.UnexId = null;
                });
            }

            results.ForEach(r =>
            {
                LdapUserContact l = new LdapService().GetUserContact(r.LoginId);
                r.Title = l.Title;
                r.Department = l.Department;
                r.Phone = l.Phone;
                r.Mobile = l.Mobile;
                r.UserName = l.UserName;
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
