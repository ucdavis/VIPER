using System.Runtime.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Controllers;
using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    [SupportedOSPlatform("windows")]
    public class AdGroupsControllerTests
    {
        [Fact]
        public async Task UpdateGroup_RejectsMismatchedGroupId_AndLeavesGroupUnchanged()
        {
            using var sqlLiteConnection = new SqliteConnection("Filename=:memory:");
            await sqlLiteConnection.OpenAsync(TestContext.Current.CancellationToken);
            var sqlLiteContextOptions = new DbContextOptionsBuilder<RAPSContext>()
                .UseSqlite(sqlLiteConnection)
                .Options;
            using var context = new RAPSContext(sqlLiteContextOptions);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            // arrange
            var ouGroup = new OuGroup { Name = "OriginalName", Description = "Original description" };
            context.OuGroups.Add(ouGroup);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var adGroupsController = new AdGroupsController(context);
            var mismatchedEdit = new GroupAddEdit
            {
                GroupId = ouGroup.OugroupId + 1,
                Name = "ChangedName",
                Description = "Changed description"
            };

            // act
            var result = await adGroupsController.UpdateGroup(ouGroup.OugroupId, mismatchedEdit);

            // assert
            Assert.IsType<BadRequestResult>(result);
            var unchangedGroup = await context.OuGroups.FindAsync(new object?[] { ouGroup.OugroupId }, TestContext.Current.CancellationToken);
            Assert.NotNull(unchangedGroup);
            Assert.Equal("OriginalName", unchangedGroup.Name);
            Assert.Equal("Original description", unchangedGroup.Description);
        }
    }
}
