using Viper.Models.VIPER;

namespace Viper.Classes
{
    public class NavMenuItem
    {
        public string MenuItemText { get; set; } = "";
        public string MenuItemURL { get; set; } = "";
        public bool IsHeader { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;

        public NavMenuItem()
        {

        }

        public NavMenuItem(LeftNavItem leftNavItem)
        {
            MenuItemText = leftNavItem.MenuItemText;
            IsHeader = leftNavItem.IsHeader;
            DisplayOrder = (int)(leftNavItem.DisplayOrder != null ? leftNavItem.DisplayOrder : 0);
            MenuItemURL = leftNavItem.Url;
        }
    }
}
