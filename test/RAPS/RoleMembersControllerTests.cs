using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Controllers;
using Viper.Classes.SQLContext;

namespace Viper.test.RAPS
{
    /// <summary>
    /// Covers the guards on the multi-role VMACS export. Both cases are rejected before the
    /// export runs, so the fixture needs contexts to construct the controller but no data.
    /// </summary>
    public class RoleMembersControllerTests : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        private RAPSContext _context = null!;

        public async ValueTask InitializeAsync()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            await _connection.OpenAsync(TestContext.Current.CancellationToken);
            _context = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
                .UseSqlite(_connection)
                .Options);
            await _context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private RoleMembersController CreateController() => new(_context);

        [Fact]
        public async Task PushRolesToVMACS_EmptyRoleIds_ReturnsBadRequest()
        {
            // act - VMACSExport reads an empty list as "no filter", so this must not reach it
            var result = await CreateController().PushRolesToVMACS("VMACS.VMTH", new List<int>());

            // assert - rejected before the export, and before the per-role CheckAccess is skipped
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task PushRolesToVMACS_NonVmacsInstance_ReturnsBadRequest()
        {
            // act
            var result = await CreateController().PushRolesToVMACS("VIPER", new List<int> { 1 });

            // assert
            Assert.IsType<BadRequestResult>(result.Result);
        }
    }
}
