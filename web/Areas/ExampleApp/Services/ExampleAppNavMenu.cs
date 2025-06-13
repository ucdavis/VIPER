using Viper.Classes.SQLContext;
using Viper.Classes;

namespace Viper.Areas.ExampleApp.Services
{
    public class ExampleAppNavMenu
    {
        private readonly RAPSContext _rapsContext;
        public ExampleAppNavMenu(RAPSContext context)
        {
            _rapsContext = context;
        }
        public NavMenu Nav()
        {
            UserHelper userHelper = new UserHelper();

            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Example App", IsHeader = true },
                new NavMenuItem() { MenuItemText = "Example Home", MenuItemURL = "ExampleHome" },
                new NavMenuItem() { MenuItemText = "Fake User Management", MenuItemURL = "FakeUsers" },
            };

            return new NavMenu("Example Application", nav);
        }
    }
}