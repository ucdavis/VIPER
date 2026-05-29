namespace Viper.Models.Crest;

/// <summary>
/// Links instructors (by PIDM) to courses (edutasks) with role information.
/// Used to determine if an instructor is the Director (IOR) for a course.
/// Read-only entity for extracting instructor role data from CREST.
/// </summary>
public class EdutaskPerson
{
    public int EdutaskId { get; set; }
    public int PersonId { get; set; }
    public string? RoleCode { get; set; }
}
