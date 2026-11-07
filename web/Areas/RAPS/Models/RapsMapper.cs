using Riok.Mapperly.Abstractions;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class RapsMapper
    {
        // Writes only the columns the role form sends, so DisplayName, UpdateFreq and
        // AllowAllUsers survive an edit. The id comes from the route, never the body.
        [MapperIgnoreSource(nameof(RoleCreateUpdate.RoleId))]
        public static partial void UpdateRole(RoleCreateUpdate source, TblRole target);
    }
}
