using Viper.Classes;

namespace Viper.test.Classes
{
    public class LeftNavHighlightTests
    {
        // Stands in for IUrlHelper.Content. The prefix mimics the "/2" PathBase used on
        // TEST and PROD, so resolution is exercised the way it behaves off the app root.
        private const string PathBase = "/2";
        private static string ResolveAppPath(string url) => PathBase + url.TrimStart('~');

        private static (int ActiveIndex, int SecondaryActiveIndex) FindActive(List<NavMenuItem> items, string requestPath)
            => LeftNavHighlight.FindActive(items, requestPath, ResolveAppPath);

        private static NavMenuItem Link(string url) => new() { MenuItemText = url, MenuItemURL = url };

        [Fact]
        public void FindActive_RelativeLinkForCurrentPage_IsPrimary()
        {
            var items = new List<NavMenuItem> { Link("Rolelist"), Link("RoleTemplateList") };

            var (active, secondary) = FindActive(items, "/2/raps/Viper/RoleTemplateList");

            Assert.Equal(1, active);
            Assert.Equal(-1, secondary);
        }

        [Fact]
        public void FindActive_ChildPage_HighlightsParentItem()
        {
            // VPR-158: RoleTemplateRoles has no nav entry, so the Role Templates item stays lit.
            var items = new List<NavMenuItem>
            {
                Link("Rolelist"),
                new() { MenuItemText = "Role Templates", MenuItemURL = "RoleTemplateList", ChildPageURLs = { "RoleTemplateRoles", "RoleTemplateApply" } }
            };

            var (active, secondary) = FindActive(items, "/2/raps/Viper/RoleTemplateRoles");

            Assert.Equal(1, active);
            Assert.Equal(-1, secondary);
        }

        [Fact]
        public void FindActive_ChildPageWithQueryString_HighlightsParentItem()
        {
            // The child page is always reached with ?roleTemplateId=, which is not part of the path.
            var items = new List<NavMenuItem>
            {
                new() { MenuItemText = "Role Templates", MenuItemURL = "RoleTemplateList", ChildPageURLs = { "RoleTemplateApply?roleTemplateId=1" } }
            };

            var (active, _) = FindActive(items, "/2/raps/Viper/RoleTemplateApply");

            Assert.Equal(0, active);
        }

        [Fact]
        public void FindActive_UnrelatedPage_HighlightsNothing()
        {
            var items = new List<NavMenuItem>
            {
                Link("Rolelist"),
                new() { MenuItemText = "Role Templates", MenuItemURL = "RoleTemplateList", ChildPageURLs = { "RoleTemplateRoles" } }
            };

            var (active, secondary) = FindActive(items, "/2/raps/Viper/AuditTrail");

            Assert.Equal(-1, active);
            Assert.Equal(-1, secondary);
        }

        [Fact]
        public void FindActive_OnlyInstanceLinkMatches_IsPromotedToPrimary()
        {
            var items = new List<NavMenuItem> { Link("~/raps/Viper/RoleList"), Link("~/raps/VMACS.VMTH/RoleList") };

            var (active, secondary) = FindActive(items, "/2/raps/Viper/RoleList");

            Assert.Equal(0, active);
            Assert.Equal(-1, secondary);
        }

        [Fact]
        public void FindActive_PageAndInstanceLinkMatch_PageLinkIsPrimary()
        {
            var items = new List<NavMenuItem> { Link("~/raps/Viper/Rolelist"), Link("Rolelist") };

            var (active, secondary) = FindActive(items, "/2/raps/Viper/Rolelist");

            Assert.Equal(1, active);
            Assert.Equal(0, secondary);
        }

        [Fact]
        public void FindActive_MatchIsCaseInsensitiveAndIgnoresTrailingSlash()
        {
            var items = new List<NavMenuItem> { Link("rolelist/") };

            var (active, _) = FindActive(items, "/2/raps/Viper/RoleList");

            Assert.Equal(0, active);
        }

        [Fact]
        public void FindActive_ExternalAndEmptyUrls_NeverMatch()
        {
            var items = new List<NavMenuItem>
            {
                new() { MenuItemText = "Header", MenuItemURL = "" },
                Link("https://ucdavis.edu/2/raps/Viper/RoleList"),
                Link("mailto:someone@ucdavis.edu")
            };

            var (active, secondary) = FindActive(items, "/2/raps/Viper/RoleList");

            Assert.Equal(-1, active);
            Assert.Equal(-1, secondary);
        }

        [Fact]
        public void FindActive_UnresolvableInstanceLink_NeverMatches()
        {
            // IUrlHelper.Content is nullable, so a URL it cannot resolve must not be
            // compared against the request path as if it had resolved to nothing.
            var items = new List<NavMenuItem> { Link("~/raps/Viper/RoleList") };

            var (active, secondary) = LeftNavHighlight.FindActive(items, "/2/raps/Viper/RoleList", _ => null);

            Assert.Equal(-1, active);
            Assert.Equal(-1, secondary);
        }

        [Fact]
        public void FindActive_RequestPathWithTrailingSlash_StillMatches()
        {
            var items = new List<NavMenuItem> { Link("RoleList") };

            var (active, _) = FindActive(items, "/2/raps/Viper/RoleList/");

            Assert.Equal(0, active);
        }

        [Fact]
        public void FindActive_RootRelativeUrl_MatchesWithoutBasePath()
        {
            // Regression guard, and not as trivial as it looks: Uri parses a leading-slash
            // path as an absolute file:// URI on Unix but not on Windows, so ordering the
            // absolute-URL check before the root-relative one passes on a dev machine and
            // fails on the Linux CI runner.
            var items = new List<NavMenuItem> { Link("/2/raps/Viper/RoleList") };

            var (active, _) = FindActive(items, "/2/raps/Viper/RoleList");

            Assert.Equal(0, active);
        }
    }
}
