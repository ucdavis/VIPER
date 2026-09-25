using Viper.Models.AAUD;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Looks up current DVM students through the AAUD students view, the one source for who counts
/// as a current student across the student self-service apps.
/// <para>
/// The PIDM and email conversions stay static on <see cref="DvmStudentLookupService"/> rather
/// than appearing here: they are pure string handling with no dependencies, and callers use them
/// without an instance.
/// </para>
/// </summary>
public interface IDvmStudentLookupService
{
    /// <summary>
    /// Loads every current DVM student, with a map from MothraId to PersonId. A student with no
    /// AaudUser row is absent from the map.
    /// </summary>
    Task<(List<VwDvmStudentsMaxTerm> DvmStudents, Dictionary<string, int> MothraToPersonId)> LoadDvmStudentsAsync();

    /// <summary>
    /// The name, class level and PIDM of one current DVM student, or null if the person is not one.
    /// </summary>
    Task<DvmStudentIdentity?> GetDvmStudentAsync(int personId);

    /// <summary>
    /// Whether this person appears in the current DVM students view. The authority on who may be
    /// written to as a student, so a save checks it before touching anything.
    /// </summary>
    Task<bool> IsCurrentDvmStudentAsync(int personId);

    /// <summary>
    /// The PIDM the student records key on, or null if the person is not a current DVM student or
    /// the view holds a PIDM that will not parse as a number.
    /// </summary>
    Task<int?> GetCurrentDvmPidmAsync(int personId);
}
