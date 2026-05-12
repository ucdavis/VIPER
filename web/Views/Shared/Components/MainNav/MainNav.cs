using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.MainNav
{
    [ViewComponent(Name = "MainNav")]
    public class MainNavViewComponent : ViewComponent
    {
        private readonly RAPSContext _context;
        private readonly string oldViperURL = HttpHelper.GetOldViperRootURL();
        //I want to iterate over a list of links to show instead of copying/pasting the link
        //[href, label, permission, tooltip?]
        private readonly List<string[]> links = new()
        {
            new[] { "/", "1.0", "SVMSecure", "VIPER 1.0" },
            new[] { "~/", "VIPER Home", "SVMSecure", "VIPER Home Page" },
            new[] { "/Accreditation/default.cfm", "Accreditation", "SVMSecure.Accreditation" },
            new[] { "/Admin/default.cfm", "Admin", "SVMSecure.admin" },
            new[] { "/Analytics/default.cfm", "Analytics", "SVMSecure.Analytics" },
            new[] { "~/CAHFS/", "CAHFS", "SVMSecure.CAHFS" },
            new[] { "/cats/default.cfm", "Computing", "SVMSecure.CATS" },
            new[] { "/curriculum/default.cfm", "Curriculum", "SVMSecure.Curriculum" },
            new[] { "/Development/default.cfm", "Development", "SVMSecure.Development" },
            new[] { "/facilities/default.cfm", "Facilities", "SVMSecure.Facilities" },
            new[] { "/faculty/default.cfm", "Faculty", "SVMSecure.Faculty" },
            new[] { "/fiscal/default.cfm", "Fiscal", "SVMSecure.Fiscal" },
            new[] { "/IDCards/default.cfm", "IDCards", "SVMSecure.IDCards.Apply" },
            new[] { "/personnel/default.cfm", "Personnel", "SVMSecure.Personnel" },
            new[] { "~/Policy", "Policies", "SVMSecure" },
            new[] { "/research/default.cfm", "Research", "SVMSecure.Research" },
            new[] { "/schedule/default.cfm", "Schedule", "SVMSecure.Schedule" },
            new[] { "~/scheduler/dashboard", "Scheduler", "SVMSecure.CATS.scheduledJobs", "Hangfire scheduler dashboard" },
            new[] { "/students/default.cfm", "Students", "SVMSecure.Students" },
            new[] { "/Hospital/default.cfm", "VMTH", "SVMSecure" },
            new[] { "https://ucdsvm.knowledgeowl.com/help", "", "", "Help" }
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
                foreach (var link in userLinks.Where(l => l[0].Substring(0, 1) == "/"))
                {
                    link[0] = "http://localhost" + link[0];
                }
            }

            ViewData["tabLinks"] = userLinks;

            var path = HttpContext.Request.Path.Value;
            var area = (path ?? "/").ToLower().Split("/");
            ViewData["SelectedTopNav"] = (area.Length >= 2 ? area[1] : area[0]) switch
            {
                "raps" => "Computing",
                "policy" => "Policies",
                "scheduler" => "Scheduler",
                _ => "VIPER Home",
            };
            return await Task.Run(() => View("Default", user));
        }

    }
}
