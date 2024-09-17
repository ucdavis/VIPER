using AutoMapper;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class AutoMapperProfileCts : Profile
    {
        public AutoMapperProfileCts() {
            CreateMap<BundleCompetency, BundleCompetencyDto>();
            CreateMap<BundleCompetencyGroup, BundleCompetencyGroupDto>();
            CreateMap<Bundle, BundleDto>()
                .ForMember(dest => dest.CompetencyCount, opt => opt.MapFrom(src => src.BundleCompetencies.Count));
            CreateMap<BundleRole, BundleRoleDto>();
            CreateMap<Role, RoleDto>().ReverseMap();
            CreateMap<Service, ServiceDto>();
            CreateMap<Level, LevelDto>();
        }
    }
}
