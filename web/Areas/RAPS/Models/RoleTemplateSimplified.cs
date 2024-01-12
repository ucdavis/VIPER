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

        public RoleTemplateSimplified() { }
        public RoleTemplateSimplified(RoleTemplate rt)
        {
            RoleTemplateId = rt.RoleTemplateId;
            TemplateName = rt.TemplateName;
            Description = rt.Description;
            Roles = new List<RoleSimplified>();
            foreach (var rtr in rt.RoleTemplateRoles)
            {
                Roles.Add(new RoleSimplified()
                {
                    RoleId = rtr.Role.RoleId,
                    FriendlyName = rtr.Role.FriendlyName
                });
            }
        }
    }

}
