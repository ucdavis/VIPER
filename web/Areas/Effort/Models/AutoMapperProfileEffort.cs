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
        // LastEmailedDate mapped from LastEmailed; LastEmailedBy (name) set by service lookup
        CreateMap<EffortPerson, PersonDto>()
            .ForMember(d => d.VolunteerWos, opt => opt.MapFrom(s => s.VolunteerWos == 1))
            .ForMember(d => d.LastEmailedDate, opt => opt.MapFrom(s => s.LastEmailed))
            .ForMember(d => d.LastEmailedBy, opt => opt.Ignore());
        CreateMap<EffortCourse, CourseDto>();

        // PercentAssignType mapping - InstructorCount set by service
        CreateMap<PercentAssignType, PercentAssignTypeDto>()
            .ForMember(d => d.InstructorCount, opt => opt.Ignore());

        // Unit mapping - UsageCount and CanDelete set by service
        CreateMap<Unit, UnitDto>()
            .ForMember(d => d.UsageCount, opt => opt.Ignore())
            .ForMember(d => d.CanDelete, opt => opt.Ignore());

        // EffortType mapping - UsageCount and CanDelete set by service
        CreateMap<EffortType, EffortTypeDto>()
            .ForMember(d => d.UsageCount, opt => opt.Ignore())
            .ForMember(d => d.CanDelete, opt => opt.Ignore());

        // EffortRecord mapping for instructor effort display
        CreateMap<EffortRecord, InstructorEffortRecordDto>()
            .ForMember(d => d.EffortType, opt => opt.MapFrom(s => s.EffortTypeId))
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.RoleId))
            .ForMember(d => d.RoleDescription, opt => opt.MapFrom(s => s.RoleNavigation.Description))
            .ForMember(d => d.ChildCourses, opt => opt.Ignore()); // Set by service

        // Percentage mapping - TypeName, TypeClass, UnitName, IsActive set by service
        CreateMap<Percentage, PercentageDto>()
            .ForMember(d => d.TypeName, opt => opt.Ignore())
            .ForMember(d => d.TypeClass, opt => opt.Ignore())
            .ForMember(d => d.UnitName, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.Ignore());
    }
}
