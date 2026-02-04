using Viper.Areas.RAPS.Controllers;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Viper.Models.AAUD;
using Viper.Areas.RAPS.Services;
using static Viper.Areas.RAPS.Services.RAPSAuditService;
using Viper.Areas.RAPS.Models;

namespace Viper.test.RAPS
{
    public class RolesTests
    {
        /// <summary>
        /// Use the _sqlLiteConnection (see RejectAddRole_WhenDuplicateRoleId() to validate unique contraints or to do create, edit, delete actions
        /// </summary>
        static SqliteConnection _sqlLiteConnection = new SqliteConnection("Filename=:memory:");
        static DbContextOptions<RAPSContext> _sqlLiteContextOptions = new DbContextOptionsBuilder<RAPSContext>()
                .UseSqlite(_sqlLiteConnection)
                .Options;

        /// <summary>
        /// Use rapsContext for Moq for cases where contraints or database edits do not need to be tested 
        /// </summary>
        readonly Mock<RAPSContext> rapsContext = new Mock<RAPSContext>();

        private IQueryable<TblRole> GetTestRoles()
        {
            var roles = new List<TblRole>
            {
                new TblRole()
                {
                    RoleId = 1,
                    Role = "VIPER.TestRole",
                    Application = 1,
                    UpdateFreq = 1,
                    AllowAllUsers = true
                },
                new TblRole()
                {
                    RoleId = 2,
                    Role = "VIPER.AnotherTestRole",
                    Application = 0,
                    UpdateFreq = 1,
                    AllowAllUsers = true
                }
            }.AsAsyncQueryable();

            return roles;
        }

        [Fact]
        public async Task ReturnNotFound_WhenNoRolesInTable()
        {
            // arrange
            var rolesController = new RolesController(rapsContext.Object);

            // act
            var roleList = await rolesController.GetTblRoles("VIPER", null);

            // assert
            Assert.IsAssignableFrom<NotFoundResult>(roleList.Result);
        }

        [Fact]
        public async Task ReturnRoles()
        {
            // arrange
            var roles = GetTestRoles();
            var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<TblRole>>();
            mockSet.As<IEnumerable<TblRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator());

            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Provider).Returns(roles.Provider);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Expression).Returns(roles.Expression);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.ElementType).Returns(roles.ElementType);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.GetEnumerator()).Returns(() => roles.GetEnumerator());

            rapsContext.Setup(c => c.TblRoles).Returns(mockSet.Object);

            var mockUser = new Mock<UserHelper>();
            mockUser.As<IUserHelper>()
                .Setup(m => m.GetCurrentUser())
                .Returns(new AaudUser());
            mockUser.As<IUserHelper>()
                .Setup(m => m.HasPermission(rapsContext.Object, It.IsAny<AaudUser>(), It.IsAny<string>()))
                .Returns(true);

            var mockSecurity = new Mock<IRAPSSecurityServiceWrapper>();
            mockSecurity.As<IRAPSSecurityServiceWrapper>()
                .Setup(m => m.IsAllowedTo(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            mockSecurity.As<IRAPSSecurityServiceWrapper>()
                .Setup(m => m.GetControlledRoleIds(It.IsAny<string>()))
                .Returns(new List<int> { 1, 2 });

            var rolesController = new RolesController(rapsContext.Object);
            rolesController.UserHelper = mockUser.Object;
            rolesController.SecurityService = mockSecurity.Object;

            // act
            var results = await rolesController.GetTblRoles("VIPER", 1);

            // assert
            Assert.IsType<ActionResult<IEnumerable<TblRole>>>(results);
            Assert.NotNull(results.Value);
            Assert.Single(results.Value);
        }

        [Fact]
        public void HelpDesk_AccessOnlyVMACSRoles()
        {
            // arrange
            var roles = SetupRoles.MockRoles.AsQueryable();
            var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<TblRole>>();
            mockSet.As<IEnumerable<TblRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator());

            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Provider).Returns(roles.Provider);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Expression).Returns(roles.Expression);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.ElementType).Returns(roles.ElementType);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.GetEnumerator()).Returns(() => roles.GetEnumerator());

            rapsContext.Setup(c => c.TblRoles).Returns(mockSet.Object);

            var mockUser = new Mock<UserHelper>();
            mockUser.As<IUserHelper>()
                .Setup(m => m.GetCurrentUser())
                .Returns(new AaudUser());
            mockUser.As<IUserHelper>()
                .Setup(m => m.HasPermission(rapsContext.Object, It.IsAny<AaudUser>(), It.IsAny<string>()))
                .Returns((RAPSContext? rc, AaudUser? user, string permission) =>
                {
                    return permission switch
                    {
                        "RAPS.Admin" => false,
                        "RAPS.EditRoleMembership" => true,
                        "RAPS.ViewRoles" => true,
                        _ => false,
                    };
                });

            var rapsSec = new RAPSSecurityService(rapsContext.Object, mockUser.Object);
            var rolesController = new RolesController(rapsContext.Object);
            rolesController.UserHelper = mockUser.Object;
            rolesController.SecurityService = new RAPSSecurityServiceWrapper(rapsSec);

            var vmacsRole = roles.ToList().Find(r => rapsSec.RoleBelongsToInstance("VMACS.VMTH", r));
            var viperRole = roles.ToList().Find(r => rapsSec.RoleBelongsToInstance("VIPER", r));

            // act
            var canViewRolePermissions = rapsSec.IsAllowedTo("ViewRolePermissions", "VMACS.VMTH", vmacsRole!);
            var canAccessVMACSRole = rapsSec.IsAllowedTo("EditRoleMembership", "VMACS.VMTH", vmacsRole!);
            var canAccessViperRole = rapsSec.IsAllowedTo("EditRoleMembership", "VIPER", viperRole!);

            // assert
            Assert.True(canViewRolePermissions, "User cannot view role permissions");
            Assert.True(canAccessVMACSRole, "User cannot access VMACS Role Membership");
            Assert.False(canAccessViperRole, "User can access VIPER Role Membership");
        }

        // TO - switch to SQLite
        [Fact]
        public async Task FindRole_WithValidId()
        {
            await _sqlLiteConnection.OpenAsync();
            using var context = new RAPSContext(_sqlLiteContextOptions);

            if (await context.Database.EnsureCreatedAsync())
            {
                // arrange
                var mockAudit = new Mock<IRAPSAuditServiceWrapper>();
                mockAudit.As<IRAPSAuditServiceWrapper>()
                    .Setup(m => m.AuditRoleChange(It.IsAny<TblRole>(), It.IsAny<AuditActionType>()));

                var rolesController = new RolesController(context);
                rolesController.AuditService = mockAudit.Object;

                RoleCreateUpdate tblRole = new RoleCreateUpdate()
                {
                    RoleId = 1,
                    Role = "VIPER.TestRole",
                    Application = 1
                };
                await rolesController.PostTblRole("VIPER", tblRole);

                // act
                var resultFound = await rolesController.GetTblRole("VIPER", 1);

                // assert
                Assert.IsType<ActionResult<TblRole>>(resultFound);
                Assert.NotNull(resultFound.Value);
                Assert.Equal(1, resultFound.Value.RoleId);
                Assert.Equal("VIPER.TestRole", resultFound.Value.Role);
                Assert.Equal(1, resultFound.Value.Application);
            }

            await _sqlLiteConnection.DisposeAsync();
        }


        [Fact]
        public async Task RejectFindRole_WithInvalidId()
        {
            // arrange
            int RoleId = -1;
            var roles = GetTestRoles();
            var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<TblRole>>();
            mockSet.As<IEnumerable<TblRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator());
            mockSet.Setup(m => m.FindAsync(RoleId))
                .ReturnsAsync(await roles.Where(r => r.RoleId == RoleId).FirstOrDefaultAsync());

            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Provider).Returns(roles.Provider);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Expression).Returns(roles.Expression);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.ElementType).Returns(roles.ElementType);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.GetEnumerator()).Returns(() => roles.GetEnumerator());

            rapsContext.Setup(c => c.TblRoles).Returns(mockSet.Object);
            var rolesController = new RolesController(rapsContext.Object);

            // act
            var result = await rolesController.GetTblRole("VIPER", RoleId);

            // assert
            Assert.IsType<ActionResult<TblRole>>(result);
            Assert.Null(result.Value);
        }


        [Fact]
        public async Task EditRole_WhenValid()
        {
            await _sqlLiteConnection.OpenAsync();
            using var context = new RAPSContext(_sqlLiteContextOptions);

            if (await context.Database.EnsureCreatedAsync())
            {
                // arrange
                var mockAudit = new Mock<IRAPSAuditServiceWrapper>();
                mockAudit.As<IRAPSAuditServiceWrapper>()
                    .Setup(m => m.AuditRoleChange(It.IsAny<TblRole>(), It.IsAny<AuditActionType>()));

                var rolesController = new RolesController(context);
                rolesController.AuditService = mockAudit.Object;

                RoleCreateUpdate tblRoleI = new RoleCreateUpdate()
                {
                    RoleId = 1,
                    Role = "VIPER.TestRole",
                    Application = 1
                };

                RoleCreateUpdate tblRoleU = new RoleCreateUpdate()
                {
                    RoleId = 1,
                    Role = "VIPER.Updated",
                    Application = 0
                };
                await rolesController.PostTblRole("VIPER", tblRoleI);

                // act
                var result = await rolesController.PutTblRole("VIPER", 1, tblRoleU);
                var resultEdited = await rolesController.GetTblRole("VIPER", 1);

                // assert
                Assert.IsType<ActionResult<TblRole>>(resultEdited);
                Assert.NotNull(resultEdited.Value);
                Assert.Equal(1, resultEdited.Value.RoleId);
                Assert.Equal("VIPER.Updated", resultEdited.Value.Role);
                Assert.Equal(0, resultEdited.Value.Application);
                Assert.IsType<NoContentResult>(result);
            }

            await _sqlLiteConnection.DisposeAsync();
        }


        [Fact]
        public async Task RejectEditRole_WhenNotFound()
        {
            // arrange
            var roles = GetTestRoles();
            var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<TblRole>>();
            mockSet.As<IEnumerable<TblRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator());

            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Provider).Returns(roles.Provider);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.Expression).Returns(roles.Expression);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.ElementType).Returns(roles.ElementType);
            mockSet.As<IQueryable<TblRole>>().Setup(m => m.GetEnumerator()).Returns(() => roles.GetEnumerator());

            rapsContext.Setup(c => c.TblRoles).Returns(mockSet.Object);
            var rolesController = new RolesController(rapsContext.Object);
            rapsContext.Setup(c => c.SetModified(It.IsAny<object>()));
            rapsContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            RoleCreateUpdate tblRole = new RoleCreateUpdate
            {
                RoleId = 1,
                Role = "VIPER.Updated",
                Application = 0
            };

            // act
            var result = await rolesController.PutTblRole("VIPER", -1, tblRole);

            // assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task AddRole_WhenValid()
        {
            await _sqlLiteConnection.OpenAsync();
            using var context = new RAPSContext(_sqlLiteContextOptions);

            if (await context.Database.EnsureCreatedAsync())
            {
                // arrange
                var mockAudit = new Mock<IRAPSAuditServiceWrapper>();
                mockAudit.As<IRAPSAuditServiceWrapper>()
                    .Setup(m => m.AuditRoleChange(It.IsAny<TblRole>(), It.IsAny<AuditActionType>()));

                var rolesController = new RolesController(context);
                rolesController.AuditService = mockAudit.Object;

                RoleCreateUpdate tblRole = new RoleCreateUpdate()
                {
                    RoleId = 1,
                    Role = "VIPER.TestRole",
                    Application = 1
                };

                // act
                var result = await rolesController.PostTblRole("VIPER", tblRole);

                // assert
                var actionResult = Assert.IsType<ActionResult<TblRole>>(result);
                var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
                var returnValue = Assert.IsType<TblRole>(createdAtActionResult.Value);
                Assert.Equal(1, returnValue.RoleId);
                Assert.Equal("VIPER.TestRole", returnValue.Role);
                Assert.Equal(1, returnValue.Application);
            }

            await _sqlLiteConnection.DisposeAsync();
        }

        [Fact]
        public async Task RejectAddRole_WhenDuplicateRoleId()
        {
            await _sqlLiteConnection.OpenAsync();
            using var context = new RAPSContext(_sqlLiteContextOptions);

            if (await context.Database.EnsureCreatedAsync())
            {
                // arrange
                var mockAudit = new Mock<IRAPSAuditServiceWrapper>();
                mockAudit.As<IRAPSAuditServiceWrapper>()
                    .Setup(m => m.AuditRoleChange(It.IsAny<TblRole>(), It.IsAny<AuditActionType>()));

                var rolesController = new RolesController(context);
                rolesController.AuditService = mockAudit.Object;

                RoleCreateUpdate tblRole = new RoleCreateUpdate
                {
                    RoleId = 1,
                    Role = "VIPER.DuplicateRoleID",
                    Application = 1
                };

                // act
                await rolesController.PostTblRole("VIPER", tblRole);

                // try to insert duplicate RoleId
                var result = await rolesController.PostTblRole("VIPER", tblRole);

                // assert
                var actionResult = Assert.IsType<ActionResult<TblRole>>(result);
                var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
                var returnValue = Assert.IsType<ProblemDetails>(objectResult.Value);

                Assert.Equal(StatusCodes.Status500InternalServerError, returnValue.Status);

            }

            await _sqlLiteConnection.DisposeAsync();
        }

        [Fact]
        public async Task DeleteRole_WhenValid()
        {
            await _sqlLiteConnection.OpenAsync();
            using var context = new RAPSContext(_sqlLiteContextOptions);

            if (await context.Database.EnsureCreatedAsync())
            {
                // arrange
                int RoleId = 1;

                var mockAudit = new Mock<IRAPSAuditServiceWrapper>();
                mockAudit.As<IRAPSAuditServiceWrapper>()
                    .Setup(m => m.AuditRoleChange(It.IsAny<TblRole>(), It.IsAny<AuditActionType>()));

                var rolesController = new RolesController(context);
                rolesController.AuditService = mockAudit.Object;

                RoleCreateUpdate tblRole = new RoleCreateUpdate()
                {
                    RoleId = 1,
                    Role = "VIPER.TestRole",
                    Application = 1
                };
                await rolesController.PostTblRole("VIPER", tblRole);

                // act
                var result = await rolesController.DeleteTblRole("VIPER", RoleId);
                var resultDeleted = await rolesController.GetTblRole("VIPER", RoleId);

                // assert
                Assert.IsType<NoContentResult>(result);
                var actionResult = Assert.IsType<ActionResult<TblRole>>(resultDeleted);
                Assert.IsType<NotFoundResult>(actionResult.Result);
                Assert.Null(resultDeleted.Value);
            }

            await _sqlLiteConnection.DisposeAsync();

        }

    }
}
