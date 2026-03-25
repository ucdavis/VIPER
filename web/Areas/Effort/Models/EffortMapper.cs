using Riok.Mapperly.Abstractions;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Models;

/// <summary>
/// Compile-time source-generated mapper for Effort area.
/// Replaces AutoMapperProfileEffort with Mapperly for zero-reflection mapping.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class EffortMapper
{
    // ── EffortTerm → TermDto ────────────────────────────────────────────────
    // TermName, TermEndDate, CanDelete, CanImportClinical set by service
    [MapperIgnoreTarget(nameof(TermDto.TermName))]
    [MapperIgnoreTarget(nameof(TermDto.TermEndDate))]
    [MapperIgnoreTarget(nameof(TermDto.CanDelete))]
    [MapperIgnoreTarget(nameof(TermDto.CanImportClinical))]
    public static partial TermDto ToTermDto(EffortTerm source);

    public static List<TermDto> ToTermDtos(IEnumerable<EffortTerm> sources)
        => sources.Select(ToTermDto).ToList();

    // ── EffortPerson → PersonDto ────────────────────────────────────────────
    // VolunteerWos: byte? → bool; LastEmailedDate ← LastEmailed; LastEmailedBy/MailId set by service
    public static PersonDto ToPersonDto(EffortPerson source)
    {
        var dto = MapPersonBase(source);
        dto.VolunteerWos = source.VolunteerWos == 1;
        dto.LastEmailedDate = source.LastEmailed;
        return dto;
    }

    public static List<PersonDto> ToPersonDtos(IEnumerable<EffortPerson> sources)
        => sources.Select(ToPersonDto).ToList();

    [MapperIgnoreTarget(nameof(PersonDto.VolunteerWos))]
    [MapperIgnoreTarget(nameof(PersonDto.LastEmailedDate))]
    [MapperIgnoreTarget(nameof(PersonDto.LastEmailedBy))]
    [MapperIgnoreTarget(nameof(PersonDto.MailId))]
    private static partial PersonDto MapPersonBase(EffortPerson source);

    // ── EffortCourse → CourseDto ────────────────────────────────────────────
    public static partial CourseDto ToCourseDto(EffortCourse source);

    public static List<CourseDto> ToCourseDtos(IEnumerable<EffortCourse> sources)
        => sources.Select(ToCourseDto).ToList();

    // ── PercentAssignType → PercentAssignTypeDto ────────────────────────────
    // InstructorCount set by service
    [MapperIgnoreTarget(nameof(PercentAssignTypeDto.InstructorCount))]
    public static partial PercentAssignTypeDto ToPercentAssignTypeDto(PercentAssignType source);

    public static List<PercentAssignTypeDto> ToPercentAssignTypeDtos(IEnumerable<PercentAssignType> sources)
        => sources.Select(ToPercentAssignTypeDto).ToList();

    // ── Unit → UnitDto ──────────────────────────────────────────────────────
    // UsageCount and CanDelete set by service
    [MapperIgnoreTarget(nameof(UnitDto.UsageCount))]
    [MapperIgnoreTarget(nameof(UnitDto.CanDelete))]
    public static partial UnitDto ToUnitDto(Unit source);

    public static List<UnitDto> ToUnitDtos(IEnumerable<Unit> sources)
        => sources.Select(ToUnitDto).ToList();

    // ── EffortType → EffortTypeDto ──────────────────────────────────────────
    // UsageCount and CanDelete set by service
    [MapperIgnoreTarget(nameof(EffortTypeDto.UsageCount))]
    [MapperIgnoreTarget(nameof(EffortTypeDto.CanDelete))]
    public static partial EffortTypeDto ToEffortTypeDto(EffortType source);

    public static List<EffortTypeDto> ToEffortTypeDtos(IEnumerable<EffortType> sources)
        => sources.Select(ToEffortTypeDto).ToList();

    // ── EffortRecord → InstructorEffortRecordDto ────────────────────────────
    // EffortType←EffortTypeId, Role←RoleId, RoleDescription←RoleNavigation.Description
    // ChildCourses set by service; Course auto-mapped via ToCourseDto
    [MapProperty("EffortTypeId", "EffortType")]
    [MapProperty("RoleId", "Role")]
    [MapProperty("RoleNavigation.Description", "RoleDescription")]
    [MapperIgnoreTarget(nameof(InstructorEffortRecordDto.ChildCourses))]
    public static partial InstructorEffortRecordDto ToInstructorEffortRecordDto(EffortRecord source);

    public static List<InstructorEffortRecordDto> ToInstructorEffortRecordDtos(IEnumerable<EffortRecord> sources)
        => sources.Select(ToInstructorEffortRecordDto).ToList();

    // ── Percentage → PercentageDto ──────────────────────────────────────────
    // TypeName, TypeClass, UnitName, IsActive set by service
    [MapperIgnoreTarget(nameof(PercentageDto.TypeName))]
    [MapperIgnoreTarget(nameof(PercentageDto.TypeClass))]
    [MapperIgnoreTarget(nameof(PercentageDto.UnitName))]
    [MapperIgnoreTarget(nameof(PercentageDto.IsActive))]
    public static partial PercentageDto ToPercentageDto(Percentage source);

    public static List<PercentageDto> ToPercentageDtos(IEnumerable<Percentage> sources)
        => sources.Select(ToPercentageDto).ToList();
}
