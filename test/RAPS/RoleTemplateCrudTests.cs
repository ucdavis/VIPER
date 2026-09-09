using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Controllers;
using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    public class RoleTemplateCrudTests
    {
        [Theory]
        [InlineData("Front desk staff", "Front desk staff")]
        [InlineData(null, "")]
        public async Task PostRoleTemplate_Creates_WhenBodyOmitsTheId(string? description, string expected)
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var controller = CreateController(context);

            var result = await controller.PostRoleTemplate("VIPER", new RoleTemplateCreateUpdate
            {
                TemplateName = "Reception",
                Description = description
            });

            var saved = Assert.IsType<RoleTemplate>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
            Assert.True(saved.RoleTemplateId > 0);
            Assert.Equal("Reception", saved.TemplateName);
            Assert.Equal(expected, saved.Description);
        }

        [Fact]
        public async Task PutRoleTemplate_Updates_WhenBodyIdMatchesRoute()
        {
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var existing = await SeedTemplateAsync(context);
            var controller = CreateController(context);

            var result = await controller.PutRoleTemplate("VIPER", existing.RoleTemplateId, new RoleTemplateCreateUpdate
            {
                RoleTemplateId = existing.RoleTemplateId,
                TemplateName = "Renamed",
                Description = "Updated"
            });

            Assert.IsType<NoContentResult>(result);

            // Reload untracked: asserting on the tracked entity would pass on an in-memory
            // mutation even if the save never reached the database.
            context.ChangeTracker.Clear();
            RoleTemplate reloaded = await context.RoleTemplates.AsNoTracking()
                .SingleAsync(t => t.RoleTemplateId == existing.RoleTemplateId, TestContext.Current.CancellationToken);
            Assert.Equal("Renamed", reloaded.TemplateName);
            Assert.Equal("Updated", reloaded.Description);
        }

        [Fact]
        public async Task PutRoleTemplate_ReturnsBadRequest_WhenBodyOmitsTheId()
        {
            // Nullable for the create form, but an update still has to agree with the route id.
            using var connection = await OpenConnectionAsync();
            using var context = await CreateContextAsync(connection);
            var existing = await SeedTemplateAsync(context);
            var controller = CreateController(context);

            var result = await controller.PutRoleTemplate("VIPER", existing.RoleTemplateId, new RoleTemplateCreateUpdate
            {
                TemplateName = "Renamed"
            });

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

        private static RoleTemplatesController CreateController(RAPSContext context)
        {
            return new RoleTemplatesController(context);
        }

        private static async Task<RoleTemplate> SeedTemplateAsync(RAPSContext context)
        {
            var template = new RoleTemplate { TemplateName = "Reception", Description = "Front desk staff" };
            context.RoleTemplates.Add(template);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return template;
        }
    }
}
