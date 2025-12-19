using AutoMapper;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Models;

public class AutoMapperProfileEffort : Profile
{
    public AutoMapperProfileEffort()
    {
        // TermName comes from lookup, CanDelete set by service
        CreateMap<EffortTerm, TermDto>()
            .ForMember(d => d.TermName, opt => opt.Ignore())
            .ForMember(d => d.CanDelete, opt => opt.Ignore());

        // All properties auto-map by convention
        CreateMap<EffortPerson, PersonDto>();
        CreateMap<EffortCourse, CourseDto>();

        // RoleDescription comes from lookup
        CreateMap<EffortRecord, RecordDto>()
            .ForMember(d => d.RoleDescription, opt => opt.Ignore());
    }
}
