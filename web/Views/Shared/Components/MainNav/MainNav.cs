using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Collections.Generic;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.MainNav
{
    [ViewComponent(Name = "MainNav")]
    public class MainNavViewComponent : ViewComponent
    {
        private RAPSContext _context;
        private readonly string oldViperURL = HttpHelper.GetOldViperRootURL();
        //I want to iterate over a list of links to show instead of copying/pasting the link
        //[href, label, permission, tooltip?]
        private readonly List<string[]> links = new()
        {
            new string[] { "/", "1.0", "SVMSecure", "VIPER 1.0" },
            new string[] { "~/", "VIPER Home", "SVMSecure", "VIPER Home Page" },
            new string[] { "/Accreditation/default.cfm", "Accreditation", "SVMSecure.Accreditation" },
            new string[] { "/Admin/default.cfm", "Admin", "SVMSecure.admin" },
            new string[] { "/Analytics/default.cfm", "Analytics", "SVMSecure.Analytics" },
            new string[] { "/cats/default.cfm", "Computing", "SVMSecure.CATS" },
            new string[] { "/curriculum/default.cfm", "Curriculum", "SVMSecure.Curriculum" },
            new string[] { "/Development/default.cfm", "Development", "SVMSecure.Development" },
            new string[] { "/facilities/default.cfm", "Facilities", "SVMSecure.Facilities" },
            new string[] { "/faculty/default.cfm", "Faculty", "SVMSecure.Faculty" },
            new string[] { "/fiscal/default.cfm", "Fiscal", "SVMSecure.Fiscal" },
            new string[] { "/IDCards/default.cfm", "IDCards", "SVMSecure.IDCards.Apply" },
            new string[] { "/personnel/default.cfm", "Personnel", "SVMSecure.Personnel" },
            new string[] { "~/Policy", "Policies", "SVMSecure" },
            new string[] { "/research/default.cfm", "Research", "SVMSecure.Research" },
            new string[] { "/schedule/default.cfm", "Schedule", "SVMSecure.Schedule" },
            new string[] { "/students/default.cfm", "Students", "SVMSecure.Students" },
            new string[] { "/Hospital/default.cfm", "VMTH", "SVMSecure" },
            new string[] { "https://ucdsvm.knowledgeowl.com/help", "", "", "Help" }
        };

        public MainNavViewComponent(RAPSContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(AaudUser user)
        {
            ViewData["OldViperURL"] = oldViperURL;
            var userHelper = new UserHelper();
            var userLinks = links.Where(l => l[2] == "" || userHelper.HasPermission(_context, user, l[2]))
                .ToList();
            
            //for local development in debugging mode, turn the absolute links to external localhost links to use port 80
            if (HttpHelper.Environment?.EnvironmentName == "Development")
            {
                foreach(var link in userLinks)
                {
                    if (link[0].Substring(0, 1) == "/")
                    {
                        link[0] = "http://localhost" + link[0];
                    }
                }
            }

            ViewData["tabLinks"] = userLinks;

            var path = HttpContext.Request.Path.Value;
            var area = (path ?? "/").ToLower().Split("/");
            ViewData["SelectedTopNav"] = (area.Length >= 2 ? area[1] : area[0]) switch
            {
                "raps" => "Computing",
                _ => "VIPER Home",
            };
            return await Task.Run(() => View("Default", user));
        }

    }
}
