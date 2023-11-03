using Microsoft.AspNetCore.Mvc;
using Viper.Models.AAUD;

namespace Viper.Views.Shared.Components.MainNav
{
    [ViewComponent(Name = "MainNav")]
    public class MainNavViewComponent : ViewComponent
    {
        private readonly string oldViperURL = HttpHelper.GetOldViperRootURL();

        public MainNavViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(AaudUser user)
        {
            ViewData["OldViperURL"] = oldViperURL;
            //I want to iterate over a list of links to show instead of copying/pasting the link
            //[href, label, permission, tooltip?]
            ViewData["tabLinks"] = new List<string[]>
            {
                new string[] { "/", "1.0", "SVMSecure", "VIPER 1.0" },
                new string[] { "~/", "VIPER Home", "SVMSecure" },
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
                new string[] { "/policy/default.cfm", "Policies", "SVMSecure" },
                new string[] { "/research/default.cfm", "Research", "SVMSecure.Research" },
                new string[] { "/schedule/default.cfm", "Schedule", "SVMSecure.Schedule" },
                new string[] { "/students/default.cfm", "Students", "SVMSecure.Students" },
                new string[] { "/Hospital/default.cfm", "VMTH", "SVMSecure" },
                new string[] { "https://ucdsvm.knowledgeowl.com/help", "", "", "Help" }
            };
            var path = HttpContext.Request.Path.Value;
            var area = (path ?? "/").ToLower().Split("/");
            switch (area.Length >= 2 ? area[1] : area[0])
            {
                case "raps":
                    ViewData["SelectedTopNav"] = "Computing";
                    break;
                default:
                    ViewData["SelectedTopNav"] = "VIPER Home";
                    break;
            }
            

            return await Task.Run(() => View("Default", user));
        }

    }
}
