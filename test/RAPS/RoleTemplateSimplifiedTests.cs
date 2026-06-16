using Viper.Areas.RAPS.Models;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    public class RoleTemplateSimplifiedTests
    {
        [Fact]
        public void Constructor_MapsScalarsAndFlattensRoles()
        {
            // arrange
            var rt = new RoleTemplate
            {
                RoleTemplateId = 42,
                TemplateName = "Test Template",
                Description = "Desc",
                RoleTemplateRoles = new List<RoleTemplateRole>
                {
                    new() { Role = new TblRole { RoleId = 1, Role = "Alpha", DisplayName = "Alpha" } },
                    new() { Role = new TblRole { RoleId = 2, Role = "Beta", DisplayName = "Beta" } }
                }
            };

            // act
            var dto = new RoleTemplateSimplified(rt);

            // assert
            Assert.Equal(42, dto.RoleTemplateId);
            Assert.Equal("Test Template", dto.TemplateName);
            Assert.Equal("Desc", dto.Description);
            var roles = dto.Roles.ToList();
            Assert.Equal(2, roles.Count);
            Assert.Equal(1, roles[0].RoleId);
            Assert.Equal("Alpha", roles[0].FriendlyName);
            Assert.Equal(2, roles[1].RoleId);
            Assert.Equal("Beta", roles[1].FriendlyName);
        }

        [Fact]
        public void Constructor_EmptyRoles_ReturnsEmptyCollection()
        {
            // arrange
            var rt = new RoleTemplate
            {
                RoleTemplateId = 7,
                TemplateName = "No Roles",
                Description = "",
                RoleTemplateRoles = new List<RoleTemplateRole>()
            };

            // act
            var dto = new RoleTemplateSimplified(rt);

            // assert
            Assert.Equal(7, dto.RoleTemplateId);
            Assert.Empty(dto.Roles);
        }
    }
}
