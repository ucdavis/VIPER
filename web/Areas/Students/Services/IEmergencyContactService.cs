using Viper.Areas.Students.Models;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Service for managing student emergency contact information.
/// </summary>
public interface IEmergencyContactService
{
    /// <summary>
    /// Gets the list of all current DVM students with their contact completeness status.
    /// </summary>
    Task<List<StudentContactListItemDto>> GetStudentContactListAsync();

    /// <summary>
    /// Gets full contact detail for a specific student by PersonId (which maps to PIDM).
    /// </summary>
    Task<StudentContactDetailDto?> GetStudentContactDetailAsync(int personId, bool canEdit);

    /// <summary>
    /// Creates or updates the contact record for a student identified by PersonId.
    /// </summary>
    Task UpdateStudentContactAsync(int personId, UpdateStudentContactRequest request, string updatedBy);

    /// <summary>
    /// Gets all student contacts formatted for a report (with pre-formatted phone numbers).
    /// </summary>
    Task<List<StudentContactReportDto>> GetStudentContactReportAsync();

    /// <summary>
    /// Returns whether the emergency contact app is open for student self-service,
    /// plus any individually granted students.
    /// </summary>
    Task<AppAccessStatusDto> GetAccessStatusAsync();

    /// <summary>
    /// Toggles the app-wide open/close for student self-service access.
    /// When the app is opened, it grants the permission to the DVM student role.
    /// When closed, it removes the role-level grant.
    /// </summary>
    Task<bool> ToggleAppAccessAsync();

    /// <summary>
    /// Toggles individual student access by PersonId.
    /// If the student already has a member permission, it is removed.
    /// Otherwise, a member permission is granted.
    /// </summary>
    Task<bool> ToggleIndividualAccessAsync(int personId);

    /// <summary>
    /// Determines whether a given user can edit contact info for the specified PersonId.
    /// Admins always can; students can if app is open or individually granted; SIS users cannot.
    /// </summary>
    Task<bool> CanEditAsync(int personId, string? currentLoginId);

    /// <summary>
    /// Checks whether the emergency contact app is currently open for students (role-level grant exists).
    /// </summary>
    Task<bool> IsAppOpenAsync();
}
