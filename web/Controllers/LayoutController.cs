using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Data;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Controllers
{
    [Route("/api/layout")]
    [Permission(Allow = "SVMSecure")]
    public class LayoutController : ApiController
    {
        private RAPSContext _context;
        private readonly string oldViperURL = HttpHelper.GetOldViperRootURL();
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
            userLinks.Add(new string[2]{ root, "root"});

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
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            NavMenu? menu = null;
            if (area)
            {
                switch (nav.ToLower())
                {
                    case "cts":
                        menu = new CtsNavMenu(_context).Nav();
                        break;
                }
                if (menu != null)
                {
                    AdjustBasePath(menu, baseUrl);
                }
            }
            if (menu == null)
            {
                menu = new LeftNavMenu().GetLeftNavMenus(friendlyName: nav)?.FirstOrDefault();
                if (menu != null)
                {
                    ConvertNavLinksForDevelopment(menu);
                    AdjustBasePath(menu, baseUrl);
                }
                else
                {
                    menu = new NavMenu("", new List<NavMenuItem>());
                }
            }

            return menu;
        }

        private static void ConvertNavLinksForDevelopment(NavMenu menu)
        {
            if (HttpHelper.Environment?.EnvironmentName == "Development" && menu?.MenuItems != null)
            {
                foreach (var item in menu.MenuItems.Where(item => item.MenuItemURL.Length > 0 && item.MenuItemURL.Substring(0, 1) == "/"))
                {
                    item.MenuItemURL = "http://localhost" + item.MenuItemURL;
                }
            }
        }

        private static void AdjustBasePath(NavMenu menu, string baseUrl)
        {
            if (menu.MenuItems != null)
            {
                foreach (var item in menu.MenuItems.Where(item => item.MenuItemURL.Length > 0 && item.MenuItemURL.Substring(0, 2) == "~/"))
                {
                    item.MenuItemURL = item.MenuItemURL.Replace("~/", baseUrl);
                }
            }
        }
    }

}
