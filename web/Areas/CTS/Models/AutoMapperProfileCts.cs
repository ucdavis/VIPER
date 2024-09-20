using AutoMapper;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class AutoMapperProfileCts : Profile
    {
        public AutoMapperProfileCts() {
            CreateMap<BundleCompetency, BundleCompetencyDto>()
                .ForMember(dest => dest.Levels, opt => opt.MapFrom(src => src.BundleCompetencyLevels.Select(bcl => bcl.Level).ToList()))
                .ForMember(dest => dest.CompetencyName, opt => opt.MapFrom(src => src.Competency.Name))
                .ForMember(dest => dest.CompetencyNumber, opt => opt.MapFrom(src => src.Competency.Number))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Competency.Description))
                .ForMember(dest => dest.CanLinkToStudent, opt => opt.MapFrom(src => src.Competency.CanLinkToStudent));
            CreateMap<BundleCompetencyGroup, BundleCompetencyGroupDto>();
            CreateMap<Bundle, BundleDto>()
                .ForMember(dest => dest.CompetencyCount, opt => opt.MapFrom(src => src.BundleCompetencies.Count))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.BundleRoles.Select(br => br.Role).ToList()))
                .ReverseMap();
            CreateMap<BundleRole, BundleRoleDto>();
            CreateMap<Role, RoleDto>().ReverseMap();
            CreateMap<Service, ServiceDto>();
            CreateMap<Level, LevelDto>();
        }
    }
}
