namespace Viper.Areas.Students.Models;

/// <summary>
/// One SVM affiliate offered by the mentor picker. The PersonId is what a career selection is
/// saved with; the IamId is carried because the shared person picker keys its options on it.
/// </summary>
public class MentorOptionDto
{
    public int PersonId { get; set; }
    public string IamId { get; set; } = string.Empty;
    /// <summary>"Last, First", matching how mentors read everywhere else in the app.</summary>
    public string FullName { get; set; } = string.Empty;
    public string? LoginId { get; set; }
    public string? MailId { get; set; }
}
