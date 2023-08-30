namespace Viper.Areas.RAPS.Models
{
    public class RoleTemplateApplyPreview
    {
        public string MemberId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public List<RoleApplyPreview> Roles { get; set; } = new List<RoleApplyPreview>();
    }
}
