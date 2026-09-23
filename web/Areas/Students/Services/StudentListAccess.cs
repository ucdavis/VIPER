using System.Diagnostics.CodeAnalysis;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Which students a roster, report or export may return.
/// </summary>
public sealed record StudentListAccess
{
    public enum AccessKind
    {
        /// <summary>No roster at all. Students reach their own record directly instead.</summary>
        Denied,
        /// <summary>Every current DVM student. Admin and read-only callers.</summary>
        AllStudents,
        /// <summary>Only the students whose career selection names this caller as their mentor.</summary>
        Mentored
    }

    public AccessKind Kind { get; }

    private string? MentorMothraId { get; }

    private StudentListAccess(AccessKind kind, string? mentorMothraId = null)
    {
        Kind = kind;
        MentorMothraId = mentorMothraId;
    }

    public static StudentListAccess Denied { get; } = new(AccessKind.Denied);

    public static StudentListAccess AllStudents { get; } = new(AccessKind.AllStudents);

    /// <summary>
    /// A faculty member's view of their own mentees. A blank MothraId mentors nobody, so it is
    /// refused here, matching <see cref="ICareerSelectionService.IsMentorOfAsync"/> on the
    /// single-record path.
    /// </summary>
    public static StudentListAccess MentoredBy(string? mentorMothraId) =>
        string.IsNullOrWhiteSpace(mentorMothraId)
            ? Denied
            : new StudentListAccess(AccessKind.Mentored, mentorMothraId);

    public bool IsDenied => Kind == AccessKind.Denied;

    /// <summary>
    /// True, with the mentor to narrow to, only when this is a mentor's view. Every other case
    /// leaves the id unset, so there is no way to read one out of an unrestricted scope.
    /// </summary>
    public bool TryGetMentor([NotNullWhen(true)] out string? mentorMothraId)
    {
        mentorMothraId = Kind == AccessKind.Mentored ? MentorMothraId : null;
        return mentorMothraId != null;
    }
}
