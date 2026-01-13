namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for an instructor with a specific percent assignment type.
/// </summary>
public class InstructorByPercentAssignTypeDto
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
public class InstructorsByPercentAssignTypeResponseDto
{
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string TypeClass { get; set; } = string.Empty;
    public List<InstructorByPercentAssignTypeDto> Instructors { get; set; } = [];
}
