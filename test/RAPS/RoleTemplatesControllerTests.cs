using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Controllers;
using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    /// <summary>
    /// Covers the Apply Template preview endpoint. The page previews as the user types,
    /// so "nobody matches that id" has to be a normal 200 result: a 404 is indistinguishable
    /// from a read that failed, and the page would show an error banner for every partially
    /// typed login id.
    /// </summary>
    public class RoleTemplatesControllerTests : IAsyncLifetime
    {
        private const int TemplateId = 1;
        private const int RoleAlreadyHeldId = 10;
        private const int RoleToAddId = 11;
        private const string KnownMothraId = "10000001";
        private const string KnownLoginId = "knownuser";

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

            var alreadyHeld = new TblRole { RoleId = RoleAlreadyHeldId, Role = "VIPER.AlreadyHeld", Description = "Held" };
            var toAdd = new TblRole { RoleId = RoleToAddId, Role = "VIPER.ToAdd", Description = "Not held" };
            _context.TblRoles.AddRange(alreadyHeld, toAdd);
            _context.RoleTemplates.Add(new RoleTemplate
            {
                RoleTemplateId = TemplateId,
                TemplateName = "VIPER Test Template",
                Description = "For tests",
                RoleTemplateRoles = new List<RoleTemplateRole>
                {
                    new() { RoleTemplateRoleRoleId = RoleAlreadyHeldId, ModBy = "test" },
                    new() { RoleTemplateRoleRoleId = RoleToAddId, ModBy = "test" }
                }
            });
            _context.VwAaudUser.Add(new VwAaudUser
            {
                AaudUserId = 1,
                MothraId = KnownMothraId,
                LoginId = KnownLoginId,
                DisplayFullName = "Known User"
            });
            _context.TblRoleMembers.Add(new TblRoleMember { RoleId = RoleAlreadyHeldId, MemberId = KnownMothraId });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private RoleTemplatesController CreateController() => new(_context);

        [Fact]
        public async Task PreviewRoleTemplateApply_UnknownMember_ReturnsOkWithNullResult()
        {
            // act
            var result = await CreateController().PreviewRoleTemplateApply("VIPER", TemplateId, "99999999");

            // assert - a 200 with no body, so the page can tell "no such user" from a failed read
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(ok.Value);
        }

        [Fact]
        public async Task PreviewRoleTemplateApply_UnknownLoginId_ReturnsOkWithNullResult()
        {
            // act
            var result = await CreateController().PreviewRoleTemplateApply("VIPER", TemplateId, "loginid:nosuchuser");

            // assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(ok.Value);
        }

        [Fact]
        public async Task PreviewRoleTemplateApply_KnownMember_FlagsRolesTheUserAlreadyHas()
        {
            // act
            var result = await CreateController().PreviewRoleTemplateApply("VIPER", TemplateId, KnownMothraId);

            // assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var preview = Assert.IsType<RoleTemplateApplyPreview>(ok.Value);
            Assert.Equal("Known User", preview.DisplayName);
            Assert.Equal(KnownMothraId, preview.MemberId);
            Assert.True(preview.Roles.Single(r => r.RoleId == RoleAlreadyHeldId).UserHasRole);
            Assert.False(preview.Roles.Single(r => r.RoleId == RoleToAddId).UserHasRole);
        }

        [Fact]
        public async Task PreviewRoleTemplateApply_KnownLoginId_ResolvesToTheSameMember()
        {
            // act
            var result = await CreateController().PreviewRoleTemplateApply("VIPER", TemplateId, "loginid:" + KnownLoginId);

            // assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var preview = Assert.IsType<RoleTemplateApplyPreview>(ok.Value);
            Assert.Equal(KnownMothraId, preview.MemberId);
        }

        [Fact]
        public async Task PreviewRoleTemplateApply_UnknownTemplate_StillReturnsNotFound()
        {
            // act - only the member lookup became a normal 200; a bad template is still a 404
            var result = await CreateController().PreviewRoleTemplateApply("VIPER", TemplateId + 999, KnownMothraId);

            // assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PreviewRoleTemplateApply_TemplateOutsideInstance_ReturnsNotFound()
        {
            // act - the VIPER template must not be reachable through the VMACS.VMTH instance
            var result = await CreateController().PreviewRoleTemplateApply("VMACS.VMTH", TemplateId, KnownMothraId);

            // assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task RoleTemplateApply_UnknownMember_ReturnsNotFound()
        {
            // act - the write has no reason to accept a member id that matches nobody
            var result = await CreateController().RoleTemplateApply("VIPER", TemplateId, "99999999");

            // assert
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
