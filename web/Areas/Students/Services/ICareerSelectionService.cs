using Viper.Areas.Students.Models;
using Viper.Models.AAUD;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Reads and writes student career selections, and the app-wide access that lets students edit
/// their own. Who may see or change a record is decided here rather than by permissions alone,
/// because a faculty mentor's reach depends on the data: see <see cref="ResolveScope"/> and
/// <see cref="IsMentorOfAsync"/>.
/// </summary>
public interface ICareerSelectionService
{
    /// <summary>
    /// Searches current SVM affiliates for the mentor picker. The affiliates view decides who is
    /// eligible, so someone who has left the school stops being offered without a data fix.
    /// A search term shorter than the minimum returns nothing rather than everyone.
    /// </summary>
    Task<List<MentorOptionDto>> SearchMentorsAsync(string? search, CancellationToken ct = default);

    /// <summary>
    /// Decides which students a caller may see. Admin and read-only outrank Faculty, so someone
    /// who holds both still sees everyone; a faculty mentor is narrowed to their own mentees.
    /// </summary>
    CareerSelectionScope ResolveScope(AaudUser currentUser);

    /// <summary>
    /// Whether a faculty member is the recorded mentor for one student. A mentor's access depends
    /// on the data rather than on their permissions, so it cannot be answered from RAPS alone.
    /// </summary>
    Task<bool> IsMentorOfAsync(string mentorMothraId, int studentPersonId);

    /// <summary>
    /// The roster view of career selections: one row per student with completion flags rather
    /// than the answers themselves.
    /// </summary>
    /// <param name="access">Which students to return. A denied scope returns nothing.</param>
    Task<List<StudentCareerListItemDto>> GetStudentCareerListAsync(StudentListAccess access);

    /// <summary>
    /// The full report of career selections, carrying each student's stored answers for export.
    /// </summary>
    /// <param name="access">Which students to return. A denied scope returns nothing.</param>
    Task<List<StudentCareerReportDto>> GetStudentCareerReportAsync(StudentListAccess access);

    /// <summary>
    /// One student's record, or null if they are not a current DVM student. A student who has no
    /// answers yet, or no PIDM to key them on, comes back as an empty shell rather than null, so
    /// the form has something to render. The two flags are carried through to the DTO for the
    /// client to lay the page out with; they do not themselves grant anything.
    /// </summary>
    Task<StudentCareerDetailDto?> GetStudentCareerDetailAsync(int personId, bool canEdit, bool canViewStudentList);

    /// <summary>
    /// Creates or updates a student's career selection. Returns the validation errors that stopped
    /// the save, or an empty list once it is saved. Only an admin save may change the mentor; a
    /// student's own save leaves whatever is stored untouched.
    /// </summary>
    Task<List<string>> UpdateStudentCareerSelectionAsync(int personId, StudentCareerInfoDto request, bool isAdmin);

    /// <summary>
    /// Whether the caller may edit this record. Takes the caller rather than their login id: the
    /// controller has already resolved them, and re-reading the row here answered the same
    /// question from a second, different read of it.
    /// </summary>
    bool CanEdit(int personId, AaudUser currentUser);

    /// <summary>
    /// Whether students can currently edit their own career selection.
    /// </summary>
    Task<bool> IsAppOpenAsync();

    /// <summary>
    /// Opens student editing if it is closed and closes it if it is open. Returns whether it is
    /// now open.
    /// </summary>
    Task<bool> ToggleAppAccessAsync();
}
