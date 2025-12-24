using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for creating a course relationship.
/// </summary>
public class CreateCourseRelationshipRequest
{
    /// <summary>
    /// The ID of the course to be linked as a child.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ChildCourseId must be a positive integer")]
    public int ChildCourseId { get; set; }

    /// <summary>
    /// The type of relationship: "CrossList" or "Section".
    /// </summary>
    [Required]
    [RegularExpression("^(CrossList|Section)$", ErrorMessage = "RelationshipType must be 'CrossList' or 'Section'")]
    public string RelationshipType { get; set; } = string.Empty;
}
