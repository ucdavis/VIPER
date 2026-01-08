namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for an instructor with a specific effort type assignment.
/// </summary>
public class InstructorByTypeDto
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO containing the type info and list of instructors with that type.
/// </summary>
public class InstructorsByTypeResponseDto
{
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string TypeClass { get; set; } = string.Empty;
    public List<InstructorByTypeDto> Instructors { get; set; } = [];
}
