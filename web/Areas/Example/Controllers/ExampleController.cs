using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Viper.Areas.Example.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Web.Authorization;

namespace Viper.Areas.Example.Controllers
{
    [Area("Example")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure")]
    public class ExampleController : AreaController
    {
        private readonly AAUDContext _AaudContext;
        public IUserHelper UserHelper;

        public ExampleController(AAUDContext context)
        {
            _AaudContext = context;
            UserHelper = new UserHelper();
        }

        /// <summary>
        /// Getting left nav for each page. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                         ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);
            await next();
            ViewData["ViperLeftNav"] = Nav();
        }

        public NavMenu Nav()
        {
            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Example Header", IsHeader = true },
                new NavMenuItem() { MenuItemText = "Home", MenuItemURL = "~/Index" },
                new NavMenuItem() { MenuItemText = "Students", MenuItemURL = "~/Students" }
            };
            return new NavMenu("Example Application", nav);
        }

        public IActionResult Index()
        {
            var currentUser = UserHelper.GetCurrentUser();
            if (currentUser != null)
            {
                ViewData["UserList"] = _AaudContext.AaudUsers
                    .Include(u => u.ExampleComment)
                    .Where(u => u.DisplayFirstName == currentUser.DisplayFirstName)
                    .OrderBy(u => u.DisplayLastName)
                    .ThenBy(u => u.MailId)
                    .ToList();
            }
            return View("~/Areas/Example/Views/Index.cshtml");
        }

        [HttpPost]
        public ActionResult Index(string comment, int aaudUserId)
        {
            ExampleComment? existing = _AaudContext.ExampleComments.Find(aaudUserId);
            if(existing != null)
            {
                existing.Comment = comment;
            }
            else
            {
                ExampleComment c = new()
                {
                    AaudUserId = aaudUserId,
                    Comment = comment
                };
                _AaudContext.Add(c);
            }
            _AaudContext.SaveChanges();
            return Redirect("~/Example/Index");
        }

        public ActionResult Students(string? classLevel)
        {
            List<string> validClassLevels = new() { "V1", "V2", "V3", "V4"};
            if(classLevel != null && !validClassLevels.Contains(classLevel))
            {
                classLevel = null;
            }
            ViewData["ClassLevel"] = classLevel ?? "V1";
            ViewData["Students"] = _AaudContext.AaudUsers
                .Where(s => (s.CurrentStudent || s.FutureStudent))
                .Select(s => new StudentClassLevelGroup()
                {
                    IamId = s.IamId,
                    Pidm = s.Pidm,
                    MailId = s.MailId,                    
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    MiddleName = s.MiddleName,
                    Student = _AaudContext.Students
                        .Where(student => student.StudentsClientid == s.SpridenId && student.StudentsTermCode == s.StudentTerm.ToString())
                        .FirstOrDefault(),
                    Studentgrp = _AaudContext.Studentgrps
                        .Where(studentgrp => studentgrp.StudentgrpPidm == s.Pidm)
                        .FirstOrDefault()
                })
                .Where(s => s.Student != null)
                .Where(s => s.Student.StudentsClassLevel == classLevel)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();
            return View("~/Areas/Example/Views/Students.cshtml");
        }
    }
}
