using Riok.Mapperly.Abstractions;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models;

/// <summary>
/// Compile-time source-generated mapper for CTS area.
/// Replaces AutoMapperProfileCts with Mapperly for zero-reflection mapping.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class CtsMapper
{
    // ── BundleCompetency → BundleCompetencyDto ──────────────────────────────
    // Levels requires LINQ projection; Description/CanLinkToStudent cross Competency nav
    public static BundleCompetencyDto ToBundleCompetencyDto(BundleCompetency source)
    {
        var dto = MapBundleCompetencyBase(source);
        dto.Levels = source.BundleCompetencyLevels?.Select(bcl => ToLevelDto(bcl.Level))
            ?? Enumerable.Empty<LevelDto>();
        dto.Description = source.Competency?.Description;
        dto.CanLinkToStudent = source.Competency?.CanLinkToStudent ?? false;
        return dto;
    }

    public static List<BundleCompetencyDto> ToBundleCompetencyDtos(IEnumerable<BundleCompetency> sources)
        => sources.Select(ToBundleCompetencyDto).ToList();

    [MapperIgnoreTarget(nameof(BundleCompetencyDto.Levels))]
    [MapperIgnoreTarget(nameof(BundleCompetencyDto.Description))]
    [MapperIgnoreTarget(nameof(BundleCompetencyDto.CanLinkToStudent))]
    private static partial BundleCompetencyDto MapBundleCompetencyBase(BundleCompetency source);

    // ── BundleCompetencyGroup → BundleCompetencyGroupDto ────────────────────
    public static partial BundleCompetencyGroupDto ToBundleCompetencyGroupDto(BundleCompetencyGroup source);

    public static List<BundleCompetencyGroupDto> ToBundleCompetencyGroupDtos(IEnumerable<BundleCompetencyGroup> sources)
        => sources.Select(ToBundleCompetencyGroupDto).ToList();

    // ── Bundle ↔ BundleDto ──────────────────────────────────────────────────
    // Manual mapping needed: CompetencyCount/Roles from navigation + required members
    public static BundleDto ToBundleDto(Bundle source)
    {
        return new BundleDto
        {
            BundleId = source.BundleId,
            Name = source.Name,
            Clinical = source.Clinical,
            Assessment = source.Assessment,
            Milestone = source.Milestone,
            CompetencyCount = source.BundleCompetencies?.Count ?? 0,
            Roles = source.BundleRoles?.Select(br => ToRoleDto(br.Role))
                ?? Enumerable.Empty<RoleDto>()
        };
    }

    public static List<BundleDto> ToBundleDtos(IEnumerable<Bundle> sources)
        => sources.Select(ToBundleDto).ToList();

    // Reverse: BundleDto → Bundle
    public static partial Bundle ToBundle(BundleDto source);

    // ── BundleRole → BundleRoleDto ──────────────────────────────────────────
    public static partial BundleRoleDto ToBundleRoleDto(BundleRole source);

    // ── Competency → CompetencyHierarchyDto ─────────────────────────────────
    // DomainName/DomainOrder auto-flatten via Domain navigation
    public static partial CompetencyHierarchyDto ToCompetencyHierarchyDto(Competency source);

    public static List<CompetencyHierarchyDto> ToCompetencyHierarchyDtos(IEnumerable<Competency> sources)
        => sources.Select(ToCompetencyHierarchyDto).ToList();

    // ── Role ↔ RoleDto ──────────────────────────────────────────────────────
    public static partial RoleDto ToRoleDto(Role source);
    public static partial Role ToRole(RoleDto source);

    public static List<RoleDto> ToRoleDtos(IEnumerable<Role> sources)
        => sources.Select(ToRoleDto).ToList();

    // ── Service → ServiceDto ────────────────────────────────────────────────
    public static partial ServiceDto ToServiceDto(Service source);

    public static List<ServiceDto> ToServiceDtos(IEnumerable<Service> sources)
        => sources.Select(ToServiceDto).ToList();

    // ── Level → LevelDto ────────────────────────────────────────────────────
    public static partial LevelDto ToLevelDto(Level source);

    public static List<LevelDto> ToLevelDtos(IEnumerable<Level> sources)
        => sources.Select(ToLevelDto).ToList();

    // ── Bundle → MilestoneDto ───────────────────────────────────────────────
    // Fully custom: MilestoneId from BundleId, competency from first BundleCompetency
    public static MilestoneDto ToMilestoneDto(Bundle source)
    {
        return new MilestoneDto
        {
            MilestoneId = source.BundleId,
            Name = source.Name,
            CompetencyName = source.BundleCompetencies.Count > 0
                ? source.BundleCompetencies.First().Competency.Name
                : null,
            CompetencyId = source.BundleCompetencies.Count > 0
                ? source.BundleCompetencies.First().Competency.CompetencyId
                : null
        };
    }

    public static List<MilestoneDto> ToMilestoneDtos(IEnumerable<Bundle> sources)
        => sources.Select(ToMilestoneDto).ToList();

    // ── MilestoneLevel → MilestoneLevelDto ──────────────────────────────────
    [MapProperty("BundleId", "MilestoneId")]
    [MapProperty("Level.LevelName", "LevelName")]
    public static partial MilestoneLevelDto ToMilestoneLevelDto(MilestoneLevel source);

    public static List<MilestoneLevelDto> ToMilestoneLevelDtos(IEnumerable<MilestoneLevel> sources)
        => sources.Select(ToMilestoneLevelDto).ToList();

    // ── Course → CourseDto ──────────────────────────────────────────────────
    public static partial CourseDto ToCourseDto(Viper.Models.CTS.Course source);

    public static List<CourseDto> ToCourseDtos(IEnumerable<Viper.Models.CTS.Course> sources)
        => sources.Select(ToCourseDto).ToList();

    // ── Session → SessionDto ────────────────────────────────────────────────
    // CompetencyCount computed from Competencies navigation
    public static SessionDto ToSessionDto(Viper.Models.CTS.Session source)
    {
        var dto = MapSessionBase(source);
        dto.CompetencyCount = source.Competencies?.Select(c => c.CompetencyId).Distinct().Count() ?? 0;
        return dto;
    }

    public static List<SessionDto> ToSessionDtos(IEnumerable<Viper.Models.CTS.Session> sources)
        => sources.Select(ToSessionDto).ToList();

    [MapperIgnoreTarget(nameof(SessionDto.CompetencyCount))]
    private static partial SessionDto MapSessionBase(Viper.Models.CTS.Session source);

    // ── SessionCompetency → SessionCompetencyDto ────────────────────────────
    // CompetencyName/CompetencyNumber/RoleName auto-flatten; CanLinkToStudent needs MapProperty
    [MapProperty("Competency.CanLinkToStudent", "CanLinkToStudent")]
    [MapperIgnoreTarget(nameof(SessionCompetencyDto.Levels))]
    public static partial SessionCompetencyDto ToSessionCompetencyDto(SessionCompetency source);

    public static List<SessionCompetencyDto> ToSessionCompetencyDtos(IEnumerable<SessionCompetency> sources)
        => sources.Select(ToSessionCompetencyDto).ToList();

    // ── LegacyCompetency → LegacyCompetencyDto ─────────────────────────────
    // Competencies requires LINQ projection from DvmCompetencyMapping
    public static LegacyCompetencyDto ToLegacyCompetencyDto(LegacyCompetency source)
    {
        var dto = MapLegacyCompetencyBase(source);
        dto.Competencies = source.DvmCompetencyMapping?.Select(d => ToCompetencyDto(d.Competency)).ToList()
            ?? new List<CompetencyDto>();
        return dto;
    }

    public static List<LegacyCompetencyDto> ToLegacyCompetencyDtos(IEnumerable<LegacyCompetency> sources)
        => sources.Select(ToLegacyCompetencyDto).ToList();

    [MapperIgnoreTarget(nameof(LegacyCompetencyDto.Competencies))]
    private static partial LegacyCompetencyDto MapLegacyCompetencyBase(LegacyCompetency source);

    // ── Competency → CompetencyDto ──────────────────────────────────────────
    // Used by LegacyCompetencyDto mapping; DomainName/DomainOrder auto-flatten
    public static partial CompetencyDto ToCompetencyDto(Competency source);

    // ── LegacySessionCompetency → LegacySessionCompetencyDto ────────────────
    public static partial LegacySessionCompetencyDto ToLegacySessionCompetencyDto(LegacySessionCompetency source);

    public static List<LegacySessionCompetencyDto> ToLegacySessionCompetencyDtos(IEnumerable<LegacySessionCompetency> sources)
        => sources.Select(ToLegacySessionCompetencyDto).ToList();
}
