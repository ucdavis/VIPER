namespace Viper.Areas.RAPS.Models
{
    public class RoleTemplateCreateUpdate
    {
        public int RoleTemplateId { get; set; }
        public string TemplateName { get; set; } = null!;
        public string? Description {  get; set; }
    }
}
