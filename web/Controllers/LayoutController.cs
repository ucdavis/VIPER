using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Data;
using Viper.Areas.CMS.Services;
using Viper.Areas.CTS.Services;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;

namespace Viper.Controllers
{
    [Route("/api/layout")]
    public class LayoutController : ApiController
    {
        private readonly RAPSContext _context;

        private readonly List<string[]> links = new()
        {
            new string[] { "/", "1.0", "SVMSecure", "VIPER 1.0" },
            new string[] { "~/", "VIPER Home", "SVMSecure", "VIPER Home Page" },
            new string[] { "/Accreditation/default.cfm", "Accreditation", "SVMSecure.Accreditation" },
            new string[] { "/Admin/default.cfm", "Admin", "SVMSecure.admin" },
            new string[] { "/Analytics/default.cfm", "Analytics", "SVMSecure.Analytics" },
            new string[] { "~/CAHFS/", "CAHFS", "SVMSecure.CAHFS" },
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

        public LayoutController(RAPSContext context)
        {
            _context = context;
        }

        [HttpGet("topnav")]
        public ActionResult<List<NavMenuItem>> GetTopNav()
        {
            var userHelper = new UserHelper();
            var user = userHelper.GetCurrentUser();
            var userLinks = links.Where(l => l[2] == "" || userHelper.HasPermission(_context, user, l[2]))
                .ToList();
            var root = Url.Content("~/");

            //for local development in debugging mode, turn the absolute links to external localhost links to use port 80
            //for ~/ (relative to Viper2 site root), use the root from the Url.Content line above to replace the literal "~/"
            foreach (var link in userLinks)
            {
                if (HttpHelper.Environment?.EnvironmentName == "Development" && link[0].Substring(0, 1) == "/")
                {
                    link[0] = "http://localhost" + link[0];
                }
                else if (link[0].Length >= 2 && link[0].Substring(0, 2) == "~/")
                {
                    link[0] = root + link[0].Replace("~/", null);
                }
            }

            return userLinks
                .Select(l => new NavMenuItem()
                {
                    MenuItemURL = l[0],
                    MenuItemText = l[1]
                })
                .ToList();
        }

        [HttpGet("leftnav")]
        public ActionResult<NavMenu> GetLeftNav(bool area = true, string nav = "viper-home")
        {
            NavMenu? menu = null;
            if (area)
            {
                switch (nav.ToLower())
                {
                    case "cts":
                        menu = new CtsNavMenu(_context).Nav();
                        break;
                    case "cms":
                        menu = new CmsNavMenu(_context).Nav();
                        break;
                    case "viper-clinical-scheduler":
                    case "clinicalscheduler":
                        menu = new ClinicalSchedulerNavMenu().Nav();
                        break;
                }
                if (menu != null)
                {
                    AdjustBasePath(menu);
                }
            }
            if (menu == null)
            {
                menu = new LeftNavMenu().GetLeftNavMenus(friendlyName: nav)?.FirstOrDefault();
                if (menu != null)
                {
                    ConvertNavLinksForDevelopment(menu);
                    AdjustBasePath(menu);
                }
                else
                {
                    menu = new NavMenu("", new List<NavMenuItem>());
                }
            }

            return menu;
        }

        /// <summary>
        /// For the development environment, links starting with '/' (but not /2) need to be changed to use http://localhost
        /// (Viper 2 will be using https locally with a custom port)
        /// </summary>
        /// <param name="menu"></param>
        private static void ConvertNavLinksForDevelopment(NavMenu menu)
        {
            if (HttpHelper.Environment?.EnvironmentName == "Development" && menu?.MenuItems != null)
            {
                foreach (var item in menu.MenuItems.Where(item => item.MenuItemURL.Length > 0
                    && item.MenuItemURL[..1] == "/"
                    && (item.MenuItemURL.Length < 2 || item.MenuItemURL[..2] != "/2")))
                {
                    item.MenuItemURL = "http://localhost" + item.MenuItemURL;
                }
            }
        }

        /// <summary>
        /// Modify the base path to handle two cases:
        ///     Links starting with "/" are VIPER 1.0 links and should be changed to start with https://{{host}}/, otherwise they will point to /2/
        ///     Links starting with "~/" or "/2" are VIPER 2.0 links and should be changed to start with "/"
        /// </summary>
        /// <param name="menu"></param>
        private static void AdjustBasePath(NavMenu menu)
        {
            if (menu.MenuItems != null)
            {
                var viper1 = HttpHelper.GetOldViperRootURL();

                // Fix VIPER 1.0 links
                foreach (var item in menu.MenuItems.Where(item => item.MenuItemURL.Length > 0
                    && item.MenuItemURL[..1] == "/"
                    && (item.MenuItemURL.Length < 2 || item.MenuItemURL[..2] != "/2")))
                {
                    item.MenuItemURL = $"{viper1}{item.MenuItemURL}";
                }

                // Fix VIPER 2.0 links
                foreach (var item in menu.MenuItems.Where(item => item.MenuItemURL.Length > 1))
                {
                    if (item.MenuItemURL[..2] == "~/")
                    {
                        item.MenuItemURL = item.MenuItemURL[1..]; //strip '~'
                    }
                    else if (item.MenuItemURL[..2] == "/2")
                    {
                        item.MenuItemURL = (item.MenuItemURL.Length > 2 ? item.MenuItemURL[2..] : ""); //strip '/2'
                    }
                }
            }
        }
    }

}
