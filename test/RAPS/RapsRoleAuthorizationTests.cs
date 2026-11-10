using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Viper.Areas.RAPS.Controllers;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using Web.Authorization;

namespace Viper.test.RAPS
{
    /// <summary>
    /// A RAPS Users member could edit roles and delegate themselves into protected roles. These run
    /// the real security service against a real schema, so the rules are exercised end to end.
    /// </summary>
    public class RapsRoleAuthorizationTests : IAsyncLifetime
    {
        private const string CallerId = "10000001";
        private const string TargetId = "10000002";
        private const int ViperRoleId = 10;
        private const int VmacsRoleId = 11;
        private const int DelegateRoleId = 12;
        private const int ControlledRoleId = 13;
        private const int VmacsTemplateId = 1;
        private const int MixedTemplateId = 2;
        private const int PermissionId = 20;
        private const string Admin = "RAPS.Admin";
        private const string HelpDesk = "RAPS.EditRoleMembership";

        /// <summary>
        /// The audit service builds its own UserHelper, which finds no user without an HttpContext,
        /// while tblLog.ModBy is required.
        /// </summary>
        private sealed class StampAuditRows : SaveChangesInterceptor
        {
            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                foreach (var entry in eventData.Context!.ChangeTracker.Entries<TblLog>().Where(e => string.IsNullOrEmpty(e.Entity.ModBy)))
                {
                    entry.Entity.ModBy = "test";
                }
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        private SqliteConnection _connection = null!;
        private RAPSContext _context = null!;
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        public async ValueTask InitializeAsync()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            await _connection.OpenAsync(Token);
            _context = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
                .UseSqlite(_connection)
                .AddInterceptors(new StampAuditRows())
                .Options);
            await _context.Database.EnsureCreatedAsync(Token);

            _context.VwAaudUser.AddRange(
                new VwAaudUser { AaudUserId = 1, MothraId = CallerId, LoginId = "caller", DisplayFullName = "Caller" },
                new VwAaudUser { AaudUserId = 2, MothraId = TargetId, LoginId = "target", DisplayFullName = "Target" });
            _context.TblRoles.AddRange(
                new TblRole { RoleId = ViperRoleId, Role = "VIPER.Protected", DisplayName = "Protected", UpdateFreq = 3, AllowAllUsers = true },
                new TblRole { RoleId = VmacsRoleId, Role = "VMACS.VMTH.Clerk" },
                new TblRole { RoleId = DelegateRoleId, Role = "VIPER.Delegate", Application = 1 },
                new TblRole { RoleId = ControlledRoleId, Role = "VIPER.Controlled" });
            _context.TblAppRoles.Add(new TblAppRole { AppRoleId = DelegateRoleId, RoleId = ControlledRoleId });
            _context.RoleTemplates.AddRange(
                new RoleTemplate
                {
                    RoleTemplateId = VmacsTemplateId,
                    TemplateName = "VMACS.VMTH Clerks",
                    Description = "",
                    RoleTemplateRoles = new List<RoleTemplateRole> { new() { RoleTemplateRoleRoleId = VmacsRoleId, ModBy = "test" } }
                },
                new RoleTemplate
                {
                    RoleTemplateId = MixedTemplateId,
                    TemplateName = "VMACS.VMTH Mixed",
                    Description = "",
                    RoleTemplateRoles = new List<RoleTemplateRole>
                    {
                        new() { RoleTemplateRoleRoleId = VmacsRoleId, ModBy = "test" },
                        new() { RoleTemplateRoleRoleId = ViperRoleId, ModBy = "test" }
                    }
                });
            _context.TblPermissions.Add(new TblPermission { PermissionId = PermissionId, Permission = "SVMSecure.Test" });
            _context.TblMemberPermissions.Add(new TblMemberPermission { PermissionId = PermissionId, MemberId = TargetId, Access = 1 });
            await _context.SaveChangesAsync(Token);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private RAPSSecurityService SecurityAs(params string[] permissions)
        {
            var userHelper = Substitute.For<IUserHelper>();
            userHelper.GetCurrentUser().Returns(new AaudUser { MothraId = CallerId });
            userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), Arg.Any<string>())
                .Returns(call => permissions.Contains(call.ArgAt<string>(2)));
            return new RAPSSecurityService(_context, userHelper);
        }

        private RolesController RolesAs(params string[] permissions) =>
            new(_context) { SecurityService = new RAPSSecurityServiceWrapper(SecurityAs(permissions)) };

        private RoleMembersController MembersAs(params string[] permissions) =>
            new(_context) { SecurityService = SecurityAs(permissions) };

        private RoleTemplatesController TemplatesAs(params string[] permissions) =>
            new(_context) { SecurityService = SecurityAs(permissions) };

        private async Task AddMembership(int roleId, string memberId, DateTime? start = null, DateTime? end = null)
        {
            _context.TblRoleMembers.Add(new TblRoleMember { RoleId = roleId, MemberId = memberId, StartDate = start, EndDate = end });
            await _context.SaveChangesAsync(Token);
        }

        private Task<List<TblRoleMember>> Memberships(string memberId) =>
            _context.TblRoleMembers.AsNoTracking().Where(rm => rm.MemberId == memberId).ToListAsync(Token);

        private static RoleMemberCreateUpdate Member(int roleId, string memberId = TargetId) =>
            new() { RoleId = roleId, MemberId = memberId };

        [Theory]
        [InlineData(nameof(RolesController.PutTblRole))]
        [InlineData(nameof(RolesController.PostTblRole))]
        [InlineData(nameof(RolesController.DeleteTblRole))]
        [InlineData(nameof(RolesController.UpdateControlledRoles))]
        [InlineData(nameof(RolesController.GetControlledRoles))]
        public void RoleAdministration_RequiresRapsAdmin(string action)
        {
            var permission = typeof(RolesController).GetMethod(action)!.GetCustomAttribute<PermissionAttribute>();

            Assert.Equal(Admin, permission?.Allow);
        }

        [Fact]
        public async Task PutTblRole_KeepsColumnsTheFormDoesNotSend()
        {
            var update = new RoleCreateUpdate { RoleId = ViperRoleId, Role = "VIPER.Renamed", Description = "New" };

            var result = await RolesAs(Admin).PutTblRole("VIPER", ViperRoleId, update);

            Assert.IsType<NoContentResult>(result);
            var stored = await _context.TblRoles.AsNoTracking().SingleAsync(r => r.RoleId == ViperRoleId, Token);
            Assert.Equal("VIPER.Renamed", stored.Role);
            Assert.Equal("New", stored.Description);
            Assert.Equal("Protected", stored.DisplayName);
            Assert.Equal(3, stored.UpdateFreq);
            Assert.True(stored.AllowAllUsers);
        }

        [Fact]
        public async Task PutTblRole_StoredRoleInAnotherInstance_ReturnsNotFoundAndLeavesItUnchanged()
        {
            // The name in the body passes the VIPER check, but the stored role is a VMACS one.
            var update = new RoleCreateUpdate { RoleId = VmacsRoleId, Role = "VIPER.Moved" };

            var result = await RolesAs(Admin).PutTblRole("VIPER", VmacsRoleId, update);

            Assert.IsType<NotFoundResult>(result);
            var stored = await _context.TblRoles.AsNoTracking().SingleAsync(r => r.RoleId == VmacsRoleId, Token);
            Assert.Equal("VMACS.VMTH.Clerk", stored.Role);
        }

        [Fact]
        public async Task GetTblRoles_AllInstances_IsIgnoredForNonAdmins()
        {
            // RAPS.RAPS.Admin catches a doubled prefix: IsAllowedTo adds "RAPS." itself.
            var result = await RolesAs("RAPS.ViewRoles", "RAPS.RAPS.Admin").GetTblRoles("VMACS.VMTH", null, allInstances: true);

            Assert.Equal(new[] { VmacsRoleId }, result.Value!.Select(r => r.RoleId));
        }

        [Fact]
        public async Task GetTblRoles_AllInstances_IsHonoredForAdmins()
        {
            var result = await RolesAs(Admin).GetTblRoles("VMACS.VMTH", null, allInstances: true);

            Assert.Contains(result.Value!, r => r.RoleId == ViperRoleId);
            Assert.Contains(result.Value!, r => r.RoleId == VmacsRoleId);
        }

        [Fact]
        public async Task PostMember_MemberRoute_BodyRoleOutsideInstance_IsRefused()
        {
            // Help desk may assign in VMACS, and must not reach a VIPER role through the body.
            var result = await MembersAs(HelpDesk).PostTblRoleMembers("VMACS.VMTH", null, TargetId, Member(ViperRoleId));

            Assert.IsType<NotFoundResult>(result.Result);
            Assert.Empty(await Memberships(TargetId));
        }

        [Fact]
        public async Task PostMember_MemberRoute_DelegateRefusedOnRoleTheyDoNotControl()
        {
            await AddMembership(DelegateRoleId, CallerId);

            var result = await MembersAs().PostTblRoleMembers("VIPER", null, TargetId, Member(ViperRoleId));

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(await Memberships(TargetId));
        }

        [Fact]
        public async Task PostMember_MemberRoute_DelegateAllowedOnRoleTheyControl()
        {
            await AddMembership(DelegateRoleId, CallerId);

            var result = await MembersAs().PostTblRoleMembers("VIPER", null, TargetId, Member(ControlledRoleId));

            // The Location link must name an action on this controller, or building it fails after the save.
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(RoleMembersController.GetTblRoleMembers), created.ActionName);
            Assert.Equal("VIPER", created.RouteValues!["instance"]);
            Assert.Equal(ControlledRoleId, Assert.Single(await Memberships(TargetId)).RoleId);
        }

        [Theory]
        [InlineData(-10, -1)]
        [InlineData(1, 10)]
        public async Task PostMember_DelegateOutsideTheirDates_IsRefused(int startOffset, int endOffset)
        {
            await AddMembership(DelegateRoleId, CallerId, DateTime.Today.AddDays(startOffset), DateTime.Today.AddDays(endOffset));

            var result = await MembersAs().PostTblRoleMembers("VIPER", ControlledRoleId, null, Member(ControlledRoleId));

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(await Memberships(TargetId));
        }

        [Fact]
        public async Task TemplateApply_InstanceTheCallerCannotEdit_IsRefused()
        {
            // Help desk holds EditRoleMembership, which only counts in VMACS instances.
            _context.RoleTemplates.Add(new RoleTemplate
            {
                RoleTemplateId = 3,
                TemplateName = "VIPER Template",
                Description = "",
                RoleTemplateRoles = new List<RoleTemplateRole> { new() { RoleTemplateRoleRoleId = ViperRoleId, ModBy = "test" } }
            });
            await _context.SaveChangesAsync(Token);

            var result = await TemplatesAs(HelpDesk).RoleTemplateApply("VIPER", 3, TargetId);

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(await Memberships(TargetId));
        }

        [Fact]
        public async Task TemplateApply_RoleTheCallerCannotGrant_RefusesTheWholeTemplate()
        {
            var result = await TemplatesAs(HelpDesk).RoleTemplateApply("VMACS.VMTH", MixedTemplateId, TargetId);

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(await Memberships(TargetId));
        }

        [Fact]
        public async Task TemplateApply_EveryRoleGrantable_AddsThem()
        {
            var result = await TemplatesAs(HelpDesk).RoleTemplateApply("VMACS.VMTH", VmacsTemplateId, TargetId);

            Assert.IsType<NoContentResult>(result.Result);
            Assert.Equal(VmacsRoleId, Assert.Single(await Memberships(TargetId)).RoleId);
        }

        [Fact]
        public async Task TemplateApply_LoginId_WritesToTheResolvedMember()
        {
            var result = await TemplatesAs(HelpDesk).RoleTemplateApply("VMACS.VMTH", VmacsTemplateId, "loginid:target");

            Assert.IsType<NoContentResult>(result.Result);
            Assert.Equal(VmacsRoleId, Assert.Single(await Memberships(TargetId)).RoleId);
        }

        [Fact]
        public async Task TemplateApply_ExpiredMembership_IsRenewed()
        {
            await AddMembership(VmacsRoleId, TargetId, DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1));

            Assert.IsType<NoContentResult>((await TemplatesAs(HelpDesk).RoleTemplateApply("VMACS.VMTH", VmacsTemplateId, TargetId)).Result);

            var membership = Assert.Single(await Memberships(TargetId));
            Assert.Null(membership.StartDate);
            Assert.Null(membership.EndDate);
            Assert.Single(await _context.TblLogs.AsNoTracking().Where(l => l.RoleId == VmacsRoleId && l.MemberId == TargetId)
                .ToListAsync(Token));
        }

        [Fact]
        public async Task TemplateApply_ActiveMembership_KeepsItsDates()
        {
            var end = DateTime.Today.AddDays(10);
            await AddMembership(VmacsRoleId, TargetId, end: end);

            Assert.IsType<NoContentResult>((await TemplatesAs(HelpDesk).RoleTemplateApply("VMACS.VMTH", VmacsTemplateId, TargetId)).Result);

            Assert.Equal(end, Assert.Single(await Memberships(TargetId)).EndDate);
        }

        [Fact]
        public async Task TemplateApply_FutureMembership_KeepsItsStartDate()
        {
            var start = DateTime.Today.AddDays(10);
            await AddMembership(VmacsRoleId, TargetId, start);

            Assert.IsType<NoContentResult>((await TemplatesAs(HelpDesk).RoleTemplateApply("VMACS.VMTH", VmacsTemplateId, TargetId)).Result);

            Assert.Equal(start, Assert.Single(await Memberships(TargetId)).StartDate);
        }

        [Fact]
        public async Task TemplatePreview_FlagsRolesTheCallerCannotGrant()
        {
            var result = await TemplatesAs(HelpDesk).PreviewRoleTemplateApply("VMACS.VMTH", MixedTemplateId, TargetId);

            var preview = Assert.IsType<RoleTemplateApplyPreview>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.True(preview.Roles.Single(r => r.RoleId == VmacsRoleId).CanGrant);
            Assert.False(preview.Roles.Single(r => r.RoleId == ViperRoleId).CanGrant);
        }

        [Theory]
        [InlineData(-10, -1, false)]
        [InlineData(1, 10, true)]
        [InlineData(-1, 1, true)]
        public async Task TemplatePreview_UserHasRole_RespectsMembershipDates(int startOffset, int endOffset, bool expected)
        {
            await AddMembership(VmacsRoleId, TargetId, DateTime.Today.AddDays(startOffset), DateTime.Today.AddDays(endOffset));

            var result = await TemplatesAs(HelpDesk).PreviewRoleTemplateApply("VMACS.VMTH", VmacsTemplateId, TargetId);

            var preview = Assert.IsType<RoleTemplateApplyPreview>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal(expected, Assert.Single(preview.Roles).UserHasRole);
        }

        [Theory]
        [InlineData(Admin, 1)]
        [InlineData("RAPS.ViewPermissions", 0)]
        [InlineData("RAPS.RAPS.Admin", 0)]
        public async Task GetTblPermissions_MemberRowsAreAdminOnly(string permission, int expected)
        {
            var controller = new PermissionsController(_context) { SecurityService = SecurityAs(permission) };

            var result = await controller.GetTblPermissions("VIPER");

            var row = Assert.Single(result.Value!);
            Assert.Equal(expected, row.TblMemberPermissions.Count);
        }
    }
}
