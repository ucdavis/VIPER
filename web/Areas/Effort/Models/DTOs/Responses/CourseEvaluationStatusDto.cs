namespace Viper.Areas.Effort.Models.DTOs.Responses;

public class CourseEvaluationStatusDto
{
    public bool CanEditAdHoc { get; set; }
    public int MaxRatingCount { get; set; }
    public List<InstructorEvalStatusDto> Instructors { get; set; } = new();
    public List<EvalCourseInfoDto> Courses { get; set; } = new();
}

public class EvalCourseInfoDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Crn { get; set; } = string.Empty;
}

public class InstructorEvalStatusDto
{
    public int PersonId { get; set; }
    public string MothraId { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public List<CourseEvalEntryDto> Evaluations { get; set; } = new();
}

public class CourseEvalEntryDto
{
    public int CourseId { get; set; }
    public string Crn { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public int? QuantId { get; set; }
    public double? Mean { get; set; }
    public double? StandardDeviation { get; set; }
    public int? Respondents { get; set; }
    public int? Count1 { get; set; }
    public int? Count2 { get; set; }
    public int? Count3 { get; set; }
    public int? Count4 { get; set; }
    public int? Count5 { get; set; }
}
