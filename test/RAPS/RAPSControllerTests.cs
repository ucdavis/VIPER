using System.Runtime.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Viper.Areas.RAPS.Controllers;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    [SupportedOSPlatform("windows")]
    public class RAPSControllerTests
    {
        [Fact]
        public async Task RoleMembers_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.RoleMembers("VIPER", 1));
        }

        [Fact]
        public async Task RolePermissions_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.RolePermissions(1));
        }

        [Fact]
        public async Task PermissionMembers_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.PermissionMembers(1));
        }

        [Fact]
        public async Task PermissionRoles_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.PermissionRoles(1));
        }

        [Fact]
        public async Task PermissionRolesRO_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.PermissionRolesRO(1));
        }

        [Fact]
        public async Task AllMembersWithPermission_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.AllMembersWithPermission(1));
        }

        [Fact]
        public async Task ExportToVMACS_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.ExportToVMACS("VMTH-test"));
        }

        [Fact]
        public async Task GroupSync_ReturnsBadRequest_WhenModelBindingFailed()
        {
            await AssertBadRequestForInvalidModelStateAsync(c => c.GroupSync(1));
        }

        [Fact]
        public async Task RolePermissions_ReturnsView_ForValidRoleId()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var controller = CreateController(context);

            var result = await controller.RolePermissions(5);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(5, view.ViewData["roleId"]);
        }

        [Fact]
        public async Task PermissionMembers_ReturnsView_WhenPermissionExists()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var permission = await SeedPermissionAsync(context);
            var controller = CreateController(context);

            var result = await controller.PermissionMembers(permission.PermissionId);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task PermissionMembers_ReturnsNotFound_WhenPermissionMissing()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var controller = CreateController(context);

            var result = await controller.PermissionMembers(9999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PermissionRoles_ReturnsView_WhenPermissionExists()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var permission = await SeedPermissionAsync(context);
            var controller = CreateController(context);

            var result = await controller.PermissionRoles(permission.PermissionId);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task PermissionRoles_ReturnsNotFound_WhenPermissionMissing()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var controller = CreateController(context);

            var result = await controller.PermissionRoles(9999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PermissionRolesRO_ReturnsView_WhenPermissionExists()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var permission = await SeedPermissionAsync(context);
            var controller = CreateController(context);

            var result = await controller.PermissionRolesRO(permission.PermissionId);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task PermissionRolesRO_ReturnsNotFound_WhenPermissionMissing()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var controller = CreateController(context);

            var result = await controller.PermissionRolesRO(9999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AllMembersWithPermission_ReturnsView_WhenPermissionExists()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var permission = await SeedPermissionAsync(context);
            var controller = CreateController(context);

            var result = await controller.AllMembersWithPermission(permission.PermissionId);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AllMembersWithPermission_ReturnsNotFound_WhenPermissionMissing()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var controller = CreateController(context);

            var result = await controller.AllMembersWithPermission(9999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GroupSync_RendersWithoutSyncing_WhenGroupMissing()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var controller = CreateController(context, scopeFactory);

            var result = await controller.GroupSync(9999);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Null(view.ViewData["Group"]);
            scopeFactory.DidNotReceive().CreateScope();
        }

        private static async Task AssertBadRequestForInvalidModelStateAsync(Func<RAPSController, Task<IActionResult>> action)
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var controller = CreateController(context);
            controller.ModelState.AddModelError("param", "The value is not valid.");

            var result = await action(controller);

            Assert.IsType<BadRequestResult>(result);
        }

        private static async Task<SqliteConnection> OpenConnectionAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            return connection;
        }

        private static async Task<RAPSContext> CreateContextAsync(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<RAPSContext>()
                .UseSqlite(connection)
                .Options;
            var context = new RAPSContext(options);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            return context;
        }

        private static RAPSController CreateController(RAPSContext context, IServiceScopeFactory? scopeFactory = null)
        {
            return new RAPSController(context, scopeFactory ?? Substitute.For<IServiceScopeFactory>());
        }

        private static async Task<TblPermission> SeedPermissionAsync(RAPSContext context)
        {
            var permission = new TblPermission { Permission = "SVMSecure.Test" };
            context.TblPermissions.Add(permission);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return permission;
        }
    }
}
