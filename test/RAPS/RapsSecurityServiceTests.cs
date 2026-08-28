using MockQueryable.NSubstitute;
using NSubstitute;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    /// <summary>
    /// Covers CanViewRoleList, which gates both the Role List nav item and the RoleList action. The nav
    /// item used to be unconditional, so a user without access saw a link that landed on a 403.
    /// </summary>
    public class RapsSecurityServiceTests
    {
        private const string MemberId = "10000001";

        private static RAPSContext ContextWithRoles(List<TblRole> roles)
        {
            // BuildMockDbSet() makes its own NSubstitute calls, so build it before opening the Returns() call
            var mockSet = roles.BuildMockDbSet();
            var context = Substitute.For<RAPSContext>();
            context.TblRoles.Returns(mockSet);
            return context;
        }

        private static IUserHelper UserWith(params string[] permissions)
        {
            var userHelper = Substitute.For<IUserHelper>();
            userHelper.GetCurrentUser().Returns(new AaudUser { MothraId = MemberId });
            userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), Arg.Any<string>())
                .Returns(call => permissions.Contains(call.ArgAt<string>(2)));
            return userHelper;
        }

        /// <summary>
        /// A delegate role (Application = 1) the user belongs to, which puts the named role under their
        /// control. The controlled role's name is what decides the instance it counts for.
        /// </summary>
        private static List<TblRole> RolesWithDelegatedRole(string controlledRole)
        {
            var controlled = new TblRole { RoleId = 2, Role = controlledRole };
            var delegateRole = new TblRole
            {
                RoleId = 1,
                Role = "VIPER.DelegateRole",
                Application = 1,
                TblRoleMembers = { new TblRoleMember { RoleId = 1, MemberId = MemberId } },
                ChildRoles = { new TblAppRole { AppRoleId = 1, RoleId = 2, Role = controlled } }
            };
            return new List<TblRole> { delegateRole, controlled };
        }

        [Fact]
        public void AdminCanViewRoleList()
        {
            var service = new RAPSSecurityService(ContextWithRoles(new List<TblRole>()), UserWith("RAPS.Admin"));

            Assert.True(service.CanViewRoleList("VIPER"));
        }

        [Theory]
        [InlineData("VMACS.VMTH", true)]
        [InlineData("VIPER", false)]
        public void HelpDeskCanViewRoleList_OnlyInVMACSInstances(string instance, bool expected)
        {
            var service = new RAPSSecurityService(ContextWithRoles(new List<TblRole>()), UserWith("RAPS.ViewRoles"));

            Assert.Equal(expected, service.CanViewRoleList(instance));
        }

        [Theory]
        [InlineData("VMACS.VMTH", true)]
        [InlineData("VIPER", false)]
        public void DelegateCanViewRoleList_OnlyInTheInstanceHoldingTheControlledRole(string instance, bool expected)
        {
            var service = new RAPSSecurityService(ContextWithRoles(RolesWithDelegatedRole("VMACS.VMTH.Controlled")), UserWith());

            // The role list filters to the instance, so a delegate whose controlled role is in
            // VMACS must not be offered the VIPER list that would come back empty.
            Assert.Equal(expected, service.CanViewRoleList(instance));
        }

        [Fact]
        public void DelegateCanViewRoleList_WithoutAnyRapsPermission()
        {
            var service = new RAPSSecurityService(ContextWithRoles(RolesWithDelegatedRole("VIPER.Controlled")), UserWith());

            Assert.True(service.CanViewRoleList("VIPER"));
        }

        [Fact]
        public void CannotViewRoleList_WithoutPermissionsOrDelegatedRoles()
        {
            var service = new RAPSSecurityService(ContextWithRoles(new List<TblRole>()), UserWith());

            Assert.False(service.CanViewRoleList("VMACS.VMTH"));
        }
    }
}
