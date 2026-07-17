using Viper.Classes;
using Viper.Models.VIPER;

namespace Test.Classes
{
    public class NavMenuItemTests
    {
        [Fact]
        public void Constructor_HeaderWithNullUrl_MapsToEmptyString()
        {
            // Headers are persisted with a null Url; Default.cshtml's header/spacer branch
            // checks MenuItemURL against "" specifically, so a null here would wrongly take
            // the link branch and fail resolving a null URL.
            var item = new NavMenuItem(new LeftNavItem { MenuItemText = "Section", IsHeader = true, Url = null });

            Assert.Equal("", item.MenuItemURL);
        }

        [Fact]
        public void Constructor_LinkWithUrl_PreservesUrl()
        {
            var item = new NavMenuItem(new LeftNavItem { MenuItemText = "Link", IsHeader = false, Url = "/somewhere" });

            Assert.Equal("/somewhere", item.MenuItemURL);
        }
    }
}
