using System.Text.Json;
using Viper.Areas.RAPS.Models;

namespace Viper.test.RAPS
{
    // Controller tests build the model in C#, so only these catch a body shape that 400s
    // during model binding, before the action runs.
    public class RoleTemplateCreateUpdateTests
    {
        [Fact]
        public void CreateBody_OmittingTheId_StillBinds()
        {
            // The create form has no id yet, so JSON.stringify drops roleTemplateId entirely.
            var model = Deserialize(@"{""templateName"":""Reception"",""description"":""Front desk staff""}");

            Assert.Null(model.RoleTemplateId);
            Assert.Equal("Reception", model.TemplateName);
            Assert.Equal("Front desk staff", model.Description);
        }

        [Fact]
        public void UpdateBody_KeepsTheId()
        {
            var model = Deserialize(@"{""roleTemplateId"":5,""templateName"":""Reception""}");

            Assert.Equal(5, model.RoleTemplateId);
        }

        // JsonSerializerOptions.Web matches how MVC binds JSON bodies.
        private static RoleTemplateCreateUpdate Deserialize(string body)
        {
            RoleTemplateCreateUpdate? model = JsonSerializer.Deserialize<RoleTemplateCreateUpdate>(body, JsonSerializerOptions.Web);

            Assert.NotNull(model);
            return model;
        }
    }
}
