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

        // VolunteerWos: byte? (1=true) → bool
        CreateMap<EffortPerson, PersonDto>()
            .ForMember(d => d.VolunteerWos, opt => opt.MapFrom(s => s.VolunteerWos == 1));
        CreateMap<EffortCourse, CourseDto>();

        // RoleDescription comes from lookup
        CreateMap<EffortRecord, RecordDto>()
            .ForMember(d => d.RoleDescription, opt => opt.Ignore());

        // EffortType mapping - InstructorCount set by service
        CreateMap<EffortType, EffortTypeDto>()
            .ForMember(d => d.InstructorCount, opt => opt.Ignore());

        // Unit mapping - UsageCount and CanDelete set by service
        CreateMap<Unit, UnitDto>()
            .ForMember(d => d.UsageCount, opt => opt.Ignore())
            .ForMember(d => d.CanDelete, opt => opt.Ignore());
    }
}
