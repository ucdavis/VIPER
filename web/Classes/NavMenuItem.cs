using Viper.Models.VIPER;

namespace Viper.Classes
{
    public class NavMenuItem
    {
        public string MenuItemText { get; set; } = "";
        public string MenuItemURL { get; set; } = "";
        public bool IsHeader { get; set; }
        public int DisplayOrder { get; set; }
        public int IndentLevel { get; set; } = 0;

        public NavMenuItem()
        {

        }

        public NavMenuItem(LeftNavItem leftNavItem)
        {
            MenuItemText = leftNavItem.MenuItemText;
            IsHeader = leftNavItem.IsHeader;
            DisplayOrder = (int)(leftNavItem.DisplayOrder != null ? leftNavItem.DisplayOrder : 0);
            // Headers are persisted with a null Url; the view's header/spacer branch checks
            // MenuItemURL against "" specifically, so a null here would wrongly take the link
            // branch and fail resolving a null URL.
            MenuItemURL = leftNavItem.Url ?? "";
        }
    }
}
