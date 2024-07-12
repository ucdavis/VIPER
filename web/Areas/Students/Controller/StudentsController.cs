﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.Students.Controller
{
    [Area("Students")]
    [Route("[area]/[action]")]
    [Authorize(Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure.Students")]
    public class StudentsController : AreaController
    {
        private readonly VIPERContext _viperContext;
        public IUserHelper UserHelper;

        public StudentsController(VIPERContext context, IWebHostEnvironment environment)
        {
            _viperContext = context;
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
                new() { MenuItemText = "Home", MenuItemURL = "Home" }
            };

            return new NavMenu("Student Resources", nav);
        }

        [Route("/[area]")]
        public IActionResult Index()
        {
            return View("~/Areas/Students/Views/Index.cshtml");
        }

        [Route("/[area]/[action]")]
        public IActionResult StudentClassYear(string? import = null, int? classYear= null)
        {
            return import != null 
                ? View("~/Areas/Students/Views/StudentClassYearImport.cshtml")
                : View("~/Areas/Students/Views/StudentClassYear.cshtml");
        }
    }
}
