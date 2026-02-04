namespace Viper.Classes
{
    public class NavMenu
    {
        public string? MenuHeaderText { get; set; }
        public List<NavMenuItem>? MenuItems { get; set; }


        public NavMenu()
        {

        }

        public NavMenu(string? menuHeaderText = null, List<NavMenuItem>? menuItems = null)
        {
            MenuHeaderText = menuHeaderText;
            MenuItems = menuItems;
        }
    }
}
