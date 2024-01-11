using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    /* Streamlined object to return from the RoleTemplate REST calls */
    public class RoleTemplateSimplified
    {
        public int RoleTemplateId { get; set; }

        public string TemplateName { get; set; } = null!;

        public string Description { get; set; } = null!;

        public virtual ICollection<RoleSimplified> Roles { get; set; } = new List<RoleSimplified>();
    }
}
