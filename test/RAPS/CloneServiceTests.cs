using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    public class CloneServiceTests
    {
        private const string SourceMemberId = "SRC00001";
        private const string TargetMemberId = "TGT00001";
        private static readonly DateTime OriginalAddDate = new(2020, 1, 15, 0, 0, 0, DateTimeKind.Local);

        /// <summary>
        /// The audit log's ModBy is non-nullable, so the service needs a current user.
        /// Granting all permissions makes IsAllowedTo("ClonePermissions") pass via its RAPS.Admin check.
        /// </summary>
        private static IUserHelper MockUserHelper(bool canClonePermissions = false)
        {
            var mockUser = Substitute.For<IUserHelper>();
            mockUser.GetCurrentUser().Returns(new AaudUser { LoginId = "testuser" });
            if (canClonePermissions)
            {
                mockUser.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), Arg.Any<string>()).Returns(true);
            }
            return mockUser;
        }

        /// <summary>
        /// Clone uses real EF change tracking (GetUserComparison tracks the target's rows, then Clone acts on them),
        /// so these tests need SQLite rather than NSubstitute mocks
        /// </summary>
        private static async Task<RAPSContext> CreateContextAsync(SqliteConnection connection)
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = new DbContextOptionsBuilder<RAPSContext>()
                .UseSqlite(connection)
                .Options;
            var context = new RAPSContext(options);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            context.VwAaudUser.AddRange(
                new VwAaudUser { MothraId = SourceMemberId, DisplayFirstName = "Source", DisplayLastName = "User", DisplayFullName = "User, Source" },
                new VwAaudUser { MothraId = TargetMemberId, DisplayFirstName = "Target", DisplayLastName = "User", DisplayFullName = "User, Target" });
            context.TblRoles.AddRange(
                new TblRole { RoleId = 1, Role = "VIPER.CloneRoleOne", Application = 0, UpdateFreq = 0, AllowAllUsers = false },
                new TblRole { RoleId = 2, Role = "VIPER.CloneRoleTwo", Application = 0, UpdateFreq = 0, AllowAllUsers = false });
            context.TblPermissions.AddRange(
                new TblPermission { PermissionId = 1, Permission = "VIPER.ClonePermissionOne" },
                new TblPermission { PermissionId = 2, Permission = "VIPER.ClonePermissionTwo" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return context;
        }

        [Fact]
        public async Task Clone_AddsRole_WhenTargetLacksIt()
        {
            // arrange
            await using var connection = new SqliteConnection("Filename=:memory:");
            using var context = await CreateContextAsync(connection);
            context.TblRoleMembers.Add(new TblRoleMember { RoleId = 1, MemberId = SourceMemberId, StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local) });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // act
            await new CloneService(context, MockUserHelper()).Clone("VIPER", SourceMemberId, TargetMemberId,
                new CloneConfirm { RoleIds = new List<int> { 1 } });

            // assert
            var targetRole = await context.TblRoleMembers.FindAsync(new object?[] { 1, TargetMemberId }, TestContext.Current.CancellationToken);
            Assert.NotNull(targetRole);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local), targetRole.StartDate);
            Assert.NotNull(targetRole.AddDate);
        }

        [Fact]
        public async Task Clone_RemovesRole_WhenSourceLacksIt()
        {
            // arrange: both users have role 1; only the target has role 2, so cloning role 2 is a Delete
            await using var connection = new SqliteConnection("Filename=:memory:");
            using var context = await CreateContextAsync(connection);
            context.TblRoleMembers.AddRange(
                new TblRoleMember { RoleId = 1, MemberId = SourceMemberId },
                new TblRoleMember { RoleId = 1, MemberId = TargetMemberId },
                new TblRoleMember { RoleId = 2, MemberId = TargetMemberId, AddDate = OriginalAddDate });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();

            // act: threw InvalidOperationException (identity conflict with the rows tracked by GetUserComparison)
            // before Clone was changed to act on tracked entities
            await new CloneService(context, MockUserHelper()).Clone("VIPER", SourceMemberId, TargetMemberId,
                new CloneConfirm { RoleIds = new List<int> { 2 } });

            // assert
            var removedRole = await context.TblRoleMembers.FindAsync(new object?[] { 2, TargetMemberId }, TestContext.Current.CancellationToken);
            Assert.Null(removedRole);
            var keptRole = await context.TblRoleMembers.FindAsync(new object?[] { 1, TargetMemberId }, TestContext.Current.CancellationToken);
            Assert.NotNull(keptRole);
        }

        [Fact]
        public async Task Clone_UpdatesDatesAndKeepsAddDate_WhenDatesDiffer()
        {
            // arrange: both users have role 1 but with different dates, so cloning role 1 is an Update
            await using var connection = new SqliteConnection("Filename=:memory:");
            using var context = await CreateContextAsync(connection);
            context.TblRoleMembers.AddRange(
                new TblRoleMember { RoleId = 1, MemberId = SourceMemberId, StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Local), EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Local) },
                new TblRoleMember { RoleId = 1, MemberId = TargetMemberId, AddDate = OriginalAddDate });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();

            // act
            await new CloneService(context, MockUserHelper()).Clone("VIPER", SourceMemberId, TargetMemberId,
                new CloneConfirm { RoleIds = new List<int> { 1 } });

            // assert: dates copied from the source, AddDate not overwritten
            context.ChangeTracker.Clear();
            var targetRole = await context.TblRoleMembers.FindAsync(new object?[] { 1, TargetMemberId }, TestContext.Current.CancellationToken);
            Assert.NotNull(targetRole);
            Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Local), targetRole.StartDate);
            Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Local), targetRole.EndDate);
            Assert.Equal(OriginalAddDate, targetRole.AddDate);
        }

        [Fact]
        public async Task Clone_AddsPermission_WhenTargetLacksIt()
        {
            // arrange
            await using var connection = new SqliteConnection("Filename=:memory:");
            using var context = await CreateContextAsync(connection);
            context.TblMemberPermissions.Add(new TblMemberPermission
            {
                PermissionId = 1,
                MemberId = SourceMemberId,
                Access = 1,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();

            // act
            await new CloneService(context, MockUserHelper(canClonePermissions: true)).Clone("VIPER", SourceMemberId, TargetMemberId,
                new CloneConfirm { PermissionIds = new List<int> { 1 } });

            // assert
            var targetPermission = await context.TblMemberPermissions.FindAsync(new object?[] { TargetMemberId, 1 }, TestContext.Current.CancellationToken);
            Assert.NotNull(targetPermission);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local), targetPermission.StartDate);
            Assert.Equal((byte)1, targetPermission.Access);
            Assert.NotNull(targetPermission.AddDate);
        }

        [Fact]
        public async Task Clone_RemovesPermission_WhenSourceLacksIt()
        {
            // arrange: both users have permission 1; only the target has permission 2, so cloning permission 2 is a Delete
            await using var connection = new SqliteConnection("Filename=:memory:");
            using var context = await CreateContextAsync(connection);
            context.TblMemberPermissions.AddRange(
                new TblMemberPermission { PermissionId = 1, MemberId = SourceMemberId, Access = 1 },
                new TblMemberPermission { PermissionId = 1, MemberId = TargetMemberId, Access = 1 },
                new TblMemberPermission { PermissionId = 2, MemberId = TargetMemberId, Access = 1, AddDate = OriginalAddDate });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();

            // act
            await new CloneService(context, MockUserHelper(canClonePermissions: true)).Clone("VIPER", SourceMemberId, TargetMemberId,
                new CloneConfirm { PermissionIds = new List<int> { 2 } });

            // assert
            var removedPermission = await context.TblMemberPermissions.FindAsync(new object?[] { TargetMemberId, 2 }, TestContext.Current.CancellationToken);
            Assert.Null(removedPermission);
            var keptPermission = await context.TblMemberPermissions.FindAsync(new object?[] { TargetMemberId, 1 }, TestContext.Current.CancellationToken);
            Assert.NotNull(keptPermission);
        }

        [Fact]
        public async Task Clone_UpdatesPermissionDatesAndAccess_WhenTheyDiffer()
        {
            // arrange: both users have permission 1 but with different dates and access flags (UpdateAndAccessFlag)
            await using var connection = new SqliteConnection("Filename=:memory:");
            using var context = await CreateContextAsync(connection);
            var sourcePermission = new TblMemberPermission
            {
                PermissionId = 1,
                MemberId = SourceMemberId,
                StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Local),
                EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Local)
            };
            context.TblMemberPermissions.AddRange(
                sourcePermission,
                new TblMemberPermission { PermissionId = 1, MemberId = TargetMemberId, Access = 1, AddDate = OriginalAddDate });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            // Access has a DB default of 1, so EF replaces a seeded 0 on insert; a deny flag must be set via update
            sourcePermission.Access = 0;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();

            // act
            await new CloneService(context, MockUserHelper(canClonePermissions: true)).Clone("VIPER", SourceMemberId, TargetMemberId,
                new CloneConfirm { PermissionIds = new List<int> { 1 } });

            // assert: dates and access copied from the source, AddDate not overwritten
            context.ChangeTracker.Clear();
            var targetPermission = await context.TblMemberPermissions.FindAsync(new object?[] { TargetMemberId, 1 }, TestContext.Current.CancellationToken);
            Assert.NotNull(targetPermission);
            Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Local), targetPermission.StartDate);
            Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Local), targetPermission.EndDate);
            Assert.Equal((byte)0, targetPermission.Access);
            Assert.Equal(OriginalAddDate, targetPermission.AddDate);
        }
    }
}
