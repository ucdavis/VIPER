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
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role == null ? null : src.Role.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Competency.Description))
                .ForMember(dest => dest.CanLinkToStudent, opt => opt.MapFrom(src => src.Competency.CanLinkToStudent));
            CreateMap<BundleCompetencyGroup, BundleCompetencyGroupDto>();
            CreateMap<Bundle, BundleDto>()
                .ForMember(dest => dest.CompetencyCount, opt => opt.MapFrom(src => src.BundleCompetencies.Count))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.BundleRoles.Select(br => br.Role).ToList()))
                .ReverseMap();
            CreateMap<BundleRole, BundleRoleDto>();
            CreateMap<Competency, CompetencyHierarchyDto>()
                .ForMember(dest => dest.DomainName, opt => opt.MapFrom(src => src.Domain.Name))
                .ForMember(dest => dest.DomainOrder, opt => opt.MapFrom(src => src.Domain.Order));
            CreateMap<Role, RoleDto>().ReverseMap();
            CreateMap<Service, ServiceDto>();
            CreateMap<Level, LevelDto>();
            CreateMap<Bundle, MilestoneDto>()
                .ForMember(dest => dest.MilestoneId, opt => opt.MapFrom(src => src.BundleId))
                .ForMember(
                    dest => dest.CompetencyName, 
                    opt => opt.MapFrom(src => 
                        src.BundleCompetencies.Count > 0 
                            ? src.BundleCompetencies.First().Competency.Name
                            : null
                    ))
                .ForMember(
                    dest => dest.CompetencyId,
                    opt => opt.MapFrom(src =>
                        src.BundleCompetencies.Count > 0
                            ? src.BundleCompetencies.First().Competency.CompetencyId
                            : (int?)null
                    ));
            CreateMap<MilestoneLevel, MilestoneLevelDto>()
                .ForMember(dest => dest.MilestoneId, opt => opt.MapFrom(src => src.BundleId))
                .ForMember(dest => dest.LevelName, opt => opt.MapFrom(src => src.Level.LevelName))
                .ForMember(dest => dest.LevelOrder, opt => opt.MapFrom(src => src.Level.Order));

            //course, session
            CreateMap<Viper.Models.CTS.Course, CourseDto>();
            CreateMap<Viper.Models.CTS.Session, SessionDto>()
                .ForMember(dest => dest.CompetencyCount, opt => opt.MapFrom(src => src.Competencies.Count()));
            CreateMap<SessionCompetency, SessionCompetencyDto>()
                .ForMember(dest => dest.SessionName, opt => opt.MapFrom(src => src.Session.Title))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Session.Type))
                .ForMember(dest => dest.TypeOrder, opt => opt.MapFrom(src => src.Session.TypeOrder))
                .ForMember(dest => dest.PaceOrder, opt => opt.MapFrom(src => src.Session.PaceOrder))
                .ForMember(dest => dest.MultiRole, opt => opt.MapFrom(src => src.Session.MultiRole))
                .ForMember(dest => dest.CompetencyName, opt => opt.MapFrom(src => src.Competency.Name))
                .ForMember(dest => dest.CompetencyNumber, opt => opt.MapFrom(src => src.Competency.Number))
                .ForMember(dest => dest.CanLinkToStudent, opt => opt.MapFrom(src => src.Competency.CanLinkToStudent))
                .ForMember(dest => dest.LevelName, opt => opt.MapFrom(src => src.Level.LevelName))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : null));

            //Legacy comps
            CreateMap<LegacyCompetency, LegacyCompetencyDto>()
                .ForMember(dest => dest.Competencies, opt => opt.MapFrom(src =>
                        src.DvmCompetencyMapping.Select(d => d.Competency).ToList()
                    ));
        }
    }
}
