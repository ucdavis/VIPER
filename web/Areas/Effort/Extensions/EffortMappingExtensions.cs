using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Extensions;

/// <summary>
/// Extension methods for mapping Effort entities to DTOs.
/// </summary>
public static class EffortMappingExtensions
{
    /// <summary>
    /// Convert an EffortTerm entity to a TermDto.
    /// </summary>
    /// <param name="term">The term entity.</param>
    /// <param name="termName">Optional human-readable term name from vwTerms.</param>
    public static TermDto ToDto(this EffortTerm term, string? termName = null)
    {
        return new TermDto
        {
            TermCode = term.TermCode,
            TermName = termName ?? term.TermCode.ToString(),
            Status = term.Status,
            HarvestedDate = term.HarvestedDate,
            OpenedDate = term.OpenedDate,
            ClosedDate = term.ClosedDate
        };
    }

    /// <summary>
    /// Convert an EffortPerson entity to a PersonDto.
    /// </summary>
    public static PersonDto ToDto(this EffortPerson person)
    {
        return new PersonDto
        {
            PersonId = person.PersonId,
            TermCode = person.TermCode,
            FirstName = person.FirstName,
            LastName = person.LastName,
            MiddleInitial = person.MiddleInitial,
            EffortTitleCode = person.EffortTitleCode,
            EffortDept = person.EffortDept,
            PercentAdmin = person.PercentAdmin,
            Title = person.Title,
            AdminUnit = person.AdminUnit,
            EffortVerified = person.EffortVerified,
            ReportUnit = person.ReportUnit,
            PercentClinical = person.PercentClinical
        };
    }

    /// <summary>
    /// Convert an EffortCourse entity to a CourseDto.
    /// </summary>
    public static CourseDto ToDto(this EffortCourse course)
    {
        return new CourseDto
        {
            Id = course.Id,
            Crn = course.Crn,
            TermCode = course.TermCode,
            SubjCode = course.SubjCode,
            CrseNumb = course.CrseNumb,
            SeqNumb = course.SeqNumb,
            Enrollment = course.Enrollment,
            Units = course.Units,
            CustDept = course.CustDept
        };
    }

    /// <summary>
    /// Convert an EffortRecord entity to a RecordDto.
    /// </summary>
    /// <param name="record">The record entity.</param>
    /// <param name="roleDescription">Optional role description from the Roles lookup.</param>
    public static RecordDto ToDto(this EffortRecord record, string? roleDescription = null)
    {
        return new RecordDto
        {
            Id = record.Id,
            CourseId = record.CourseId,
            PersonId = record.PersonId,
            TermCode = record.TermCode,
            SessionType = record.SessionType,
            Role = record.Role,
            RoleDescription = roleDescription ?? record.Role.ToString(),
            Hours = record.Hours,
            Weeks = record.Weeks,
            Crn = record.Crn,
            ModifiedDate = record.ModifiedDate
        };
    }
}
